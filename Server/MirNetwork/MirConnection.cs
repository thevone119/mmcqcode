﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using Server.MirDatabase;
using Server.MirEnvir;
using Server.MirObjects;
using C = ClientPackets;
using S = ServerPackets;
using System.Linq;

namespace Server.MirNetwork
{
    //游戏状态
    public enum GameStage { None, Login, Select, Game, Disconnected }

    //游戏链接类
    public class MirConnection
    {
        //回话ID
        public readonly int SessionID;
        //IP
        public readonly string IPAddress;
        //游戏状态
        public GameStage Stage;
        //客户端连接
        private TcpClient _client;
        //接受数据包
        private ConcurrentQueue<Packet> _receiveList;
        //发送数据包
        private ConcurrentQueue<Packet> _sendList; 
        //重试发送数据包？
        private Queue<Packet> _retryList;
        //是否正在断开连接
        private bool _disconnecting;
        //是否连接
        public bool Connected;
        public bool Disconnecting
        {
            get { return _disconnecting; }
            set
            {
                if (_disconnecting == value) return;
                _disconnecting = value;
                TimeOutTime = SMain.Envir.Time + 500;
            }
        }
        //连接的时间
        public readonly long TimeConnected;
        //断开连接时间，超时时间
        public long TimeDisconnected, TimeOutTime;
        //接受的数据，多余的都放在这里？
        byte[] _rawData = new byte[0];
        //账号信息
        public AccountInfo Account;
        //玩家信息
        public PlayerObject Player;
        //物品列表
        public List<ItemInfo> SentItemInfo = new List<ItemInfo>();
        //查询信息
        public List<QuestInfo> SentQuestInfo = new List<QuestInfo>();
        //食物信息
        public List<RecipeInfo> SentRecipeInfo = new List<RecipeInfo>();
        //存储发送
        public bool StorageSent;





        //初始化
        public MirConnection(int sessionID, TcpClient client)
        {
            SessionID = sessionID;
            IPAddress = client.Client.RemoteEndPoint.ToString().Split(':')[0];

            int connCount = 0;
            for (int i = 0; i < SMain.Envir.Connections.Count; i++)
            {
                MirConnection conn = SMain.Envir.Connections[i];
                if (conn.IPAddress == IPAddress && conn.Connected)
                {
                    connCount++;
                    if (connCount >= Settings.MaxIP)
                    {
                        SMain.EnqueueDebugging(IPAddress + ", Maximum connections reached.");
                        conn.SendDisconnect(5);
                    }
                }
            }

            SMain.Enqueue(IPAddress + ", Connected.");

            _client = client;
            _client.NoDelay = true;

            TimeConnected = SMain.Envir.Time;
            TimeOutTime = TimeConnected + Settings.TimeOut;


            _receiveList = new ConcurrentQueue<Packet>();
            _sendList = new ConcurrentQueue<Packet>();
            _sendList.Enqueue(new S.Connected());
            _retryList = new Queue<Packet>();

            Connected = true;
            BeginReceive();
        }
        //开始接受数据，异步
        private void BeginReceive()
        {
            if (!Connected) return;

            byte[] rawBytes = new byte[8 * 1024];

            try
            {
                _client.Client.BeginReceive(rawBytes, 0, rawBytes.Length, SocketFlags.None, ReceiveData, rawBytes);
            }
            catch
            {
                Disconnecting = true;
            }
        }
        //接受到数据，并处理
        private void ReceiveData(IAsyncResult result)
        {
            if (!Connected) return;

            int dataRead;

            try
            {
                dataRead = _client.Client.EndReceive(result);
            }
            catch
            {
                Disconnecting = true;
                return;
            }

            if (dataRead == 0)
            {
                Disconnecting = true;
                return;
            }

            byte[] rawBytes = result.AsyncState as byte[];

            byte[] temp = _rawData;
            _rawData = new byte[dataRead + temp.Length];
            Buffer.BlockCopy(temp, 0, _rawData, 0, temp.Length);
            Buffer.BlockCopy(rawBytes, 0, _rawData, temp.Length, dataRead);

            Packet p;
            while ((p = Packet.ReceivePacket(_rawData, out _rawData)) != null)
                _receiveList.Enqueue(p);

            BeginReceive();
        }
        //开始发送数据，异步
        private void BeginSend(List<byte> data)
        {
            if (!Connected || data.Count == 0) return;

            //Interlocked.Add(ref Network.Sent, data.Count);

            try
            {
                _client.Client.BeginSend(data.ToArray(), 0, data.Count, SocketFlags.None, SendData, Disconnecting);
            }
            catch
            {
                Disconnecting = true;
            }
        }
        //发送数据完成后的处理
        private void SendData(IAsyncResult result)
        {
            try
            {
                _client.Client.EndSend(result);
            }
            catch
            { }
        }

        //把发送数据放入队列
        public void Enqueue(Packet p)
        {
            if (_sendList != null && p != null)
                _sendList.Enqueue(p);
        }

        //循环处理所有的数据
        //这个适合线程死循环
        public void Process()
        {
            try
            {
                if (_client == null || !_client.Connected)
                {
                    Disconnect(20);
                    return;
                }

                while (!_receiveList.IsEmpty && !Disconnecting)
                {
                    Packet p;
                    if (!_receiveList.TryDequeue(out p)) continue;
                    TimeOutTime = SMain.Envir.Time + Settings.TimeOut;
                    ProcessPacket(p);

                    if (_receiveList == null)
                        return;
                }

                while (_retryList.Count > 0)
                    _receiveList.Enqueue(_retryList.Dequeue());

                if (SMain.Envir.Time > TimeOutTime)
                {
                    Disconnect(21);
                    return;
                }

                if (_sendList == null || _sendList.Count <= 0) return;

                List<byte> data = new List<byte>();
                while (!_sendList.IsEmpty)
                {
                    Packet p;
                    if (!_sendList.TryDequeue(out p) || p == null) continue;
                    data.AddRange(p.GetPacketBytes());
                }

                BeginSend(data);
            }
            catch(Exception e)
            {
                SMain.Enqueue(e);
            }
        }
        //对数据包进行处理
        private void ProcessPacket(Packet p)
        {
            if (p == null || Disconnecting) return;

            switch (p.Index)
            {
                case (short)ClientPacketIds.ClientVersion:
                    ClientVersion((C.ClientVersion) p);
                    break;
                case (short)ClientPacketIds.Disconnect:
                    Disconnect(22);
                    break;
                case (short)ClientPacketIds.KeepAlive: // Keep Alive
                    ClientKeepAlive((C.KeepAlive)p);
                    break;
                case (short)ClientPacketIds.NewAccount:
                    NewAccount((C.NewAccount) p);
                    break;
                case (short)ClientPacketIds.ChangePassword:
                    ChangePassword((C.ChangePassword) p);
                    break;
                case (short)ClientPacketIds.Login:
                    Login((C.Login) p);
                    break;
                case (short)ClientPacketIds.NewCharacter:
                    NewCharacter((C.NewCharacter) p);
                    break;
  
                case (short)ClientPacketIds.DeleteCharacter:
                    DeleteCharacter((C.DeleteCharacter) p);
                    break;
                case (short)ClientPacketIds.StartGame:
                    StartGame((C.StartGame) p);
                    break;
                case (short)ClientPacketIds.LogOut:
                    LogOut();
                    break;
                case (short)ClientPacketIds.Turn:
                    Turn((C.Turn) p);
                    break;
                case (short)ClientPacketIds.Walk:
                    Walk((C.Walk) p);
                    break;
                case (short)ClientPacketIds.Run:
                    Run((C.Run) p);
                    break;
                case (short)ClientPacketIds.Chat:
                    Chat((C.Chat) p);
                    break;
                case (short)ClientPacketIds.MoveItem:
                    MoveItem((C.MoveItem) p);
                    break;
                case (short)ClientPacketIds.StoreItem:
                    StoreItem((C.StoreItem) p);
                    break;
                case (short)ClientPacketIds.DepositRefineItem:
                    DepositRefineItem((C.DepositRefineItem)p);
                    break;
                case (short)ClientPacketIds.RetrieveRefineItem:
                    RetrieveRefineItem((C.RetrieveRefineItem)p);
                    break;
                case (short)ClientPacketIds.RefineCancel:
                    RefineCancel((C.RefineCancel)p);
                    break;
                case (short)ClientPacketIds.RefineItem:
                    RefineItem((C.RefineItem)p);
                    break;
                case (short)ClientPacketIds.CheckRefine:
                    CheckRefine((C.CheckRefine)p);
                    break;
                case (short)ClientPacketIds.ReplaceWedRing:
                    ReplaceWedRing((C.ReplaceWedRing)p);
                    break;
                case (short)ClientPacketIds.DepositTradeItem:
                    DepositTradeItem((C.DepositTradeItem)p);
                    break;
                case (short)ClientPacketIds.RetrieveTradeItem:
                    RetrieveTradeItem((C.RetrieveTradeItem)p);
                    break;
                case (short)ClientPacketIds.TakeBackItem:
                    TakeBackItem((C.TakeBackItem) p);
                    break;
                case (short)ClientPacketIds.MergeItem:
                    MergeItem((C.MergeItem) p);
                    break;
                case (short)ClientPacketIds.EquipItem:
                    EquipItem((C.EquipItem) p);
                    break;
                case (short)ClientPacketIds.RemoveItem:
                    RemoveItem((C.RemoveItem) p);
                    break;
                case (short)ClientPacketIds.RemoveSlotItem:
                    RemoveSlotItem((C.RemoveSlotItem)p);
                    break;
                case (short)ClientPacketIds.SplitItem:
                    SplitItem((C.SplitItem) p);
                    break;
                case (short)ClientPacketIds.UseItem:
                    UseItem((C.UseItem) p);
                    break;
                case (short)ClientPacketIds.DropItem:
                    DropItem((C.DropItem) p);
                    break;
                case (short)ClientPacketIds.DropGold:
                    DropGold((C.DropGold) p);
                    break;
                case (short)ClientPacketIds.PickUp:
                    PickUp();
                    break;
                case (short)ClientPacketIds.Inspect:
                    Inspect((C.Inspect)p);
                    break;
                case (short)ClientPacketIds.ChangeAMode:
                    ChangeAMode((C.ChangeAMode)p);
                    break;
                case (short)ClientPacketIds.ChangePMode:
                    ChangePMode((C.ChangePMode)p);
                    break;
                case (short)ClientPacketIds.ChangeTrade:
                    ChangeTrade((C.ChangeTrade)p);
                    break;
                case (short)ClientPacketIds.Attack:
                    Attack((C.Attack)p);
                    break;
                case (short)ClientPacketIds.RangeAttack:
                    RangeAttack((C.RangeAttack)p);
                    break;
                case (short)ClientPacketIds.Harvest:
                    Harvest((C.Harvest)p);
                    break;
                case (short)ClientPacketIds.CallNPC:
                    CallNPC((C.CallNPC)p);
                    break;
                case (short)ClientPacketIds.TalkMonsterNPC:
                    TalkMonsterNPC((C.TalkMonsterNPC)p);
                    break;
                case (short)ClientPacketIds.BuyItem:
                    BuyItem((C.BuyItem)p);
                    break;
                case (short)ClientPacketIds.CraftItem:
                    CraftItem((C.CraftItem)p);
                    break;
                case (short)ClientPacketIds.SellItem:
                    SellItem((C.SellItem)p);
                    break;
                case (short)ClientPacketIds.RepairItem:
                    RepairItem((C.RepairItem)p);
                    break;
                case (short)ClientPacketIds.BuyItemBack:
                    BuyItemBack((C.BuyItemBack)p);
                    break;
                case (short)ClientPacketIds.SRepairItem:
                    SRepairItem((C.SRepairItem)p);
                    break;
                case (short)ClientPacketIds.MagicKey:
                    MagicKey((C.MagicKey)p);
                    break;
                case (short)ClientPacketIds.Magic:
                    Magic((C.Magic)p);
                    break;
                case (short)ClientPacketIds.SwitchGroup:
                    SwitchGroup((C.SwitchGroup)p);
                    return;
                case (short)ClientPacketIds.AddMember:
                    AddMember((C.AddMember)p);
                    return;
                case (short)ClientPacketIds.DellMember:
                    DelMember((C.DelMember)p);
                    return;
                case (short)ClientPacketIds.GroupInvite:
                    GroupInvite((C.GroupInvite)p);
                    return;
                case (short)ClientPacketIds.TownRevive:
                    TownRevive();
                    return;
                case (short)ClientPacketIds.SpellToggle:
                    SpellToggle((C.SpellToggle)p);
                    return;
                case (short)ClientPacketIds.ConsignItem:
                    ConsignItem((C.ConsignItem)p);
                    return;
                case (short)ClientPacketIds.MarketSearch:
                    MarketSearch((C.MarketSearch)p);
                    return;
    
                case (short)ClientPacketIds.MarketBuy:
                    MarketBuy((C.MarketBuy)p);
                    return;
                case (short)ClientPacketIds.MarketGetBack:
                    MarketGetBack((C.MarketGetBack)p);
                    return;
                case (short)ClientPacketIds.RequestUserName:
                    RequestUserName((C.RequestUserName)p);
                    return;
                case (short)ClientPacketIds.RequestChatItem:
                    RequestChatItem((C.RequestChatItem)p);
                    return;
                case (short)ClientPacketIds.EditGuildMember:
                    EditGuildMember((C.EditGuildMember)p);
                    return;
                case (short)ClientPacketIds.EditGuildNotice:
                    EditGuildNotice((C.EditGuildNotice)p);
                    return;
                case (short)ClientPacketIds.GuildInvite:
                    GuildInvite((C.GuildInvite)p);
                    return;
                case (short)ClientPacketIds.RequestGuildInfo:
                    RequestGuildInfo((C.RequestGuildInfo)p);
                    return;
                case (short)ClientPacketIds.GuildNameReturn:
                    GuildNameReturn((C.GuildNameReturn)p);
                    return;
                case (short)ClientPacketIds.GuildStorageGoldChange:
                    GuildStorageGoldChange((C.GuildStorageGoldChange)p);
                    return;
                case (short)ClientPacketIds.GuildStorageItemChange:
                    GuildStorageItemChange((C.GuildStorageItemChange)p);
                    return;
                case (short)ClientPacketIds.GuildWarReturn:
                    GuildWarReturn((C.GuildWarReturn)p);
                    return;
                case (short)ClientPacketIds.MarriageRequest:
                    MarriageRequest((C.MarriageRequest)p);
                    return;
                case (short)ClientPacketIds.MarriageReply:
                    MarriageReply((C.MarriageReply)p);
                    return;
                case (short)ClientPacketIds.ChangeMarriage:
                    ChangeMarriage((C.ChangeMarriage)p);
                    return;
                case (short)ClientPacketIds.DivorceRequest:
                    DivorceRequest((C.DivorceRequest)p);
                    return;
                case (short)ClientPacketIds.DivorceReply:
                    DivorceReply((C.DivorceReply)p);
                    return;
                case (short)ClientPacketIds.AddMentor:
                    AddMentor((C.AddMentor)p);
                    return;
                case (short)ClientPacketIds.MentorReply:
                    MentorReply((C.MentorReply)p);
                    return;
                case (short)ClientPacketIds.AllowMentor:
                    AllowMentor((C.AllowMentor)p);
                    return;
                case (short)ClientPacketIds.CancelMentor:
                    CancelMentor((C.CancelMentor)p);
                    return;
                case (short)ClientPacketIds.TradeRequest:
                    TradeRequest((C.TradeRequest)p);
                    return;
                case (short)ClientPacketIds.TradeGold:
                    TradeGold((C.TradeGold)p);
                    return;
                case (short)ClientPacketIds.TradeReply:
                    TradeReply((C.TradeReply)p);
                    return;
                case (short)ClientPacketIds.TradeConfirm:
                    TradeConfirm((C.TradeConfirm)p);
                    return;
                case (short)ClientPacketIds.TradeCancel:
                    TradeCancel((C.TradeCancel)p);
                    return;
                case (short)ClientPacketIds.EquipSlotItem:
                    EquipSlotItem((C.EquipSlotItem)p);
                    break;
                case (short)ClientPacketIds.FishingCast:
                    FishingCast((C.FishingCast)p);
                    break;
                case (short)ClientPacketIds.FishingChangeAutocast:
                    FishingChangeAutocast((C.FishingChangeAutocast)p);
                    break;
                case (short)ClientPacketIds.AcceptQuest:
                    AcceptQuest((C.AcceptQuest)p);
                    break;
                case (short)ClientPacketIds.FinishQuest:
                    FinishQuest((C.FinishQuest)p);
                    break;
                case (short)ClientPacketIds.AbandonQuest:
                    AbandonQuest((C.AbandonQuest)p);
                    break;
                case (short)ClientPacketIds.ShareQuest:
                    ShareQuest((C.ShareQuest)p);
                    break;
                case (short)ClientPacketIds.AcceptReincarnation:
                    AcceptReincarnation();
                    break;
                case (short)ClientPacketIds.CancelReincarnation:
                     CancelReincarnation();
                    break;
                case (short)ClientPacketIds.CombineItem:
                    CombineItem((C.CombineItem)p);
                    break;
                case (short)ClientPacketIds.SetConcentration:
                    SetConcentration((C.SetConcentration)p);
                    break;
                case (short)ClientPacketIds.AwakeningNeedMaterials:
                    AwakeningNeedMaterials((C.AwakeningNeedMaterials)p);
                    break;
                case (short)ClientPacketIds.AwakeningLockedItem:
                    Enqueue(new S.AwakeningLockedItem { UniqueID = ((C.AwakeningLockedItem)p).UniqueID, Locked = ((C.AwakeningLockedItem)p).Locked });
                    break;
                case (short)ClientPacketIds.Awakening:
                    Awakening((C.Awakening)p);
                    break;
                case (short)ClientPacketIds.DisassembleItem:
                    DisassembleItem((C.DisassembleItem)p);
                    break;
                case (short)ClientPacketIds.DowngradeAwakening:
                    DowngradeAwakening((C.DowngradeAwakening)p);
                    break;
                case (short)ClientPacketIds.ResetAddedItem:
                    ResetAddedItem((C.ResetAddedItem)p);
                    break;
                case (short)ClientPacketIds.SendMail:
                    SendMail((C.SendMail)p);
                    break;
                case (short)ClientPacketIds.ReadMail:
                    ReadMail((C.ReadMail)p);
                    break;
                case (short)ClientPacketIds.CollectParcel:
                    CollectParcel((C.CollectParcel)p);
                    break;
                case (short)ClientPacketIds.DeleteMail:
                    DeleteMail((C.DeleteMail)p);
                    break;
                case (short)ClientPacketIds.LockMail:
                    LockMail((C.LockMail)p);
                    break;
                case (short)ClientPacketIds.MailLockedItem:
                    Enqueue(new S.MailLockedItem { UniqueID = ((C.MailLockedItem)p).UniqueID, Locked = ((C.MailLockedItem)p).Locked });
                    break;
                case (short)ClientPacketIds.MailCost:
                    MailCost((C.MailCost)p);
                    break;
                case (short)ClientPacketIds.UpdateIntelligentCreature://IntelligentCreature
                    UpdateIntelligentCreature((C.UpdateIntelligentCreature)p);
                    break;
                case (short)ClientPacketIds.IntelligentCreaturePickup://IntelligentCreature
                    IntelligentCreaturePickup((C.IntelligentCreaturePickup)p);
                    break;
                case (short)ClientPacketIds.AddFriend:
                    AddFriend((C.AddFriend)p);
                    break;
                case (short)ClientPacketIds.RemoveFriend:
                    RemoveFriend((C.RemoveFriend)p);
                    break;
                case (short)ClientPacketIds.RefreshFriends:
                    {
                        if (Stage != GameStage.Game) return;
                        Player.GetFriends();
                        break;
                    }
                case (short)ClientPacketIds.AddMemo:
                    AddMemo((C.AddMemo)p);
                    break;
                case (short)ClientPacketIds.GuildBuffUpdate:
                    GuildBuffUpdate((C.GuildBuffUpdate)p);
                    break;
                case (short)ClientPacketIds.GameshopBuy:
                    GameshopBuy((C.GameshopBuy)p);
                    return;
                case (short)ClientPacketIds.NPCConfirmInput:
                    NPCConfirmInput((C.NPCConfirmInput)p);
                    break;
                case (short)ClientPacketIds.ReportIssue:
                    ReportIssue((C.ReportIssue)p);
                    break;
                case (short)ClientPacketIds.GetRanking:
                    GetRanking((C.GetRanking)p);
                    break;
                case (short)ClientPacketIds.Opendoor:
                    Opendoor((C.Opendoor)p);
                    break;
                case (short)ClientPacketIds.GetRentedItems:
                    GetRentedItems();
                    break;
                case (short)ClientPacketIds.ItemRentalRequest:
                    ItemRentalRequest();
                    break;
                case (short)ClientPacketIds.ItemRentalFee:
                    ItemRentalFee((C.ItemRentalFee)p);
                    break;
                case (short)ClientPacketIds.ItemRentalPeriod:
                    ItemRentalPeriod((C.ItemRentalPeriod)p);
                    break;
                case (short)ClientPacketIds.DepositRentalItem:
                    DepositRentalItem((C.DepositRentalItem)p);
                    break;
                case (short)ClientPacketIds.RetrieveRentalItem:
                    RetrieveRentalItem((C.RetrieveRentalItem)p);
                    break;
                case (short)ClientPacketIds.CancelItemRental:
                    CancelItemRental();
                    break;
                case (short)ClientPacketIds.ItemRentalLockFee:
                    ItemRentalLockFee();
                    break;
                case (short)ClientPacketIds.ItemRentalLockItem:
                    ItemRentalLockItem();
                    break;
                case (short)ClientPacketIds.ConfirmItemRental:
                    ConfirmItemRental();
                    break;
                case (short)ClientPacketIds.RefreshUserGold:
                    RefreshUserGold();
                    break;
                case (short)ClientPacketIds.RechargeCredit:
                    RechargeCredit((C.RechargeCredit)p);
                    break;
                case (short)ClientPacketIds.RechargeEnd:
                    RechargeEnd((C.RechargeEnd)p);
                    break;
                case (short)ClientPacketIds.RefreshInventory:
                    RefreshInventory((C.RefreshInventory)p);
                    break;
                case (short)ClientPacketIds.DepositItemCollect:
                    DepositItemCollect((C.DepositItemCollect)p);
                    break;
                case (short)ClientPacketIds.RetrieveItemCollect:
                    RetrieveItemCollect((C.RetrieveItemCollect)p);
                    break;
                case (short)ClientPacketIds.ItemCollectCancel:
                    ItemCollectCancel((C.ItemCollectCancel)p);
                    break;
                case (short)ClientPacketIds.ConfirmItemCollect:
                    ConfirmItemCollect((C.ConfirmItemCollect)p);
                    break;
                case (short)ClientPacketIds.MagicParameter:
                    MagicParameter((C.MagicParameter)p);
                    break;


                case (short)ClientPacketIds.MyMonsterOperation:
                    MyMonsterOperation((C.MyMonsterOperation)p);
                    break;

                case (short)ClientPacketIds.CheckCode:
                    CheckCode((C.CheckCode)p);
                    break;
                case (short)ClientPacketIds.RefreshCheckCode:
                    SendCheckCode(1);
                    break;
                case (short)ClientPacketIds.ClientSubmitProcess://客户端提交进程数据
                    ClientSubmitProcess((C.ClientSubmitProcess)p);
                    break;
                case (short)ClientPacketIds.ClientSubmitFrame://客户端提交画面，截图
                    ClientSubmitFrame((C.ClientSubmitFrame)p);
                    break;

                default:
                    SMain.Enqueue(string.Format("Invalid packet received. Index : {0}", p.Index));
                    break;
            }
        }
        //断开连接
        public void SoftDisconnect(byte reason)
        {
            Stage = GameStage.Disconnected;
            TimeDisconnected = SMain.Envir.Time;
            
            lock (Envir.AccountLock)
            {
                if (Player != null)
                    Player.StopGame(reason);

                if (Account != null && Account.Connection == this)
                    Account.Connection = null;
            }

            Account = null;
        }
        //断开连接
        public void Disconnect(byte reason)
        {
            if (!Connected) return;

            Connected = false;
            Stage = GameStage.Disconnected;
            TimeDisconnected = SMain.Envir.Time;

            lock (SMain.Envir.Connections)
                SMain.Envir.Connections.Remove(this);

            lock (Envir.AccountLock)
            {
                if (Player != null)
                    Player.StopGame(reason);

                if (Account != null && Account.Connection == this)
                    Account.Connection = null;

            }

            Account = null;

            _sendList = null;
            _receiveList = null;
            _retryList = null;
            _rawData = null;

            if (_client != null) _client.Client.Dispose();
            _client = null;
        }
        //让客户端断开连接
        public void SendDisconnect(byte reason)
        {
            if (!Connected)
            {
                Disconnecting = true;
                SoftDisconnect(reason);
                return;
            }
            
            Disconnecting = true;

            List<byte> data = new List<byte>();

            data.AddRange(new S.Disconnect { Reason = reason }.GetPacketBytes());

            BeginSend(data);
            SoftDisconnect(reason);
        }

        //这里发送校验码
        //src 0:登录，1：小退 3：游戏内
        public void SendCheckCode(byte src)
        {
            if (!Settings.openCheckCode)
            {
                return;
            }
            //获取
            if (Account == null)
            {
                return;
            }
            
            //1秒只发一个验证码
            if (Account.LastSendCodeTime < SMain.Envir.Time)
            {
                Account.LastSendCodeTime = SMain.Envir.Time + (Settings.Second);
            }
            else
            {
                return;
            }

            //最后3秒，不允许重发
            if (Account.LastCheckTime>0 && Account.LastCheckTime -3000 < SMain.Envir.Time)
            {
                return;
            }

            //1分钟内校验
            if (Account.LastCheckTime < SMain.Envir.Time)
            {
                Account.LastCheckTime = SMain.Envir.Time + (Settings.Minute);
            }
            Account.checkErrorCount++;
            SMain.Enqueue("SendCheckCode...:"+Account.AccountID);
            //下次校验时间也更新
            Account.NextCheckTime = Account.LastCheckTime+10000;
            //发送校验码
            Account.CheckCode = RandomUtils.RandomomRangeChineseTerm();

            Enqueue(new S.CheckCode { code = EncryptHelper.DesEncrypt(Account.CheckCode +"_" + RandomUtils.Next()), remainTime= Account.LastCheckTime-SMain.Envir.Time});

        }

        //校验客户端发送上来的校验码
        public void CheckCode(C.CheckCode p)
        {
            //获取
            if (Account == null)
            {
                return;
            }
            
            //超时或者校验不通过，都失败，关闭客户端
            if (p==null||Account.LastCheckTime < SMain.Envir.Time || p.code!= Account.CheckCode)
            {
                SMain.Enqueue("CheckCode error...:" + Account.AccountID);
                Account.LastCheckTime = 0;
                SendDisconnect(6);
                return;
            }
            SMain.Enqueue("CheckCode suss...:"+ Account.AccountID);
            Account.LastCheckTime = 0;
            Account.checkErrorCount--;
            Account.checkSuccCount++;
            //验证通过了，更新下次验证时间(如果错得多，则时间短)
            if (Account.checkSuccCount <= Account.checkErrorCount)
            {
                Account.NextCheckTime = SMain.Envir.Time + (Settings.Minute * RandomUtils.Next(60, 120));
                return;
            }
            //错误次数非常少，则时间延长些
            if(Account.checkErrorCount<= Account.checkSuccCount / 8)
            {
                Account.NextCheckTime = SMain.Envir.Time + (Settings.Minute * RandomUtils.Next(120, 240));
                return;
            }

            Account.NextCheckTime = SMain.Envir.Time + (Settings.Minute * RandomUtils.Next(120, 180));
            return;

        }

        //客户端提交进程数据
        private void ClientSubmitProcess(C.ClientSubmitProcess p)
        {
            if (Account == null)
            {
                return;
            }
            SMain.Enqueue(Account.AccountID+ " SubmitProcess:" + p.processs);
        }

        //客户端提交画面，截图
        private void ClientSubmitFrame(C.ClientSubmitFrame p)
        {
            if (Account == null)
            {
                return;
            }
            SMain.Enqueue(Account.AccountID + " SubmitProcess");
            p.AccountID = Account.AccountID;
            SMain.Enqueue(p);
        }

        //检测客户端版本
        private void ClientVersion(C.ClientVersion p)
        {
            if (Stage != GameStage.None) return;

            if (Settings.CheckVersion)
                if (!Functions.CompareBytes(Settings.VersionHash, p.VersionHash))
                {
                    Disconnecting = true;

                    List<byte> data = new List<byte>();

                    data.AddRange(new S.ClientVersion {Result = 0}.GetPacketBytes());

                    BeginSend(data);
                    SoftDisconnect(10);
                    SMain.Enqueue(SessionID + ", Disconnnected - Wrong Client Version.");
                    return;
                }


            if (Settings.ISStopIp(IPAddress))
            {
                Disconnecting = true;
                List<byte> data = new List<byte>();

                data.AddRange(new S.ClientVersion { Result = 0 }.GetPacketBytes());

                BeginSend(data);
                SoftDisconnect(10);
                SMain.EnqueueDebugging(IPAddress + ", ISStopIp.");
                SMain.Enqueue(SessionID + ", Disconnnected - Wrong Client Version.");
                return;
            }

            SMain.Enqueue(SessionID + ", " + IPAddress + ", Client version matched.");
            Enqueue(new S.ClientVersion { Result = 1 });

            Stage = GameStage.Login;
        }
        private void ClientKeepAlive(C.KeepAlive p)
        {
            Enqueue(new S.KeepAlive
            {
                Time = p.Time
            });
        }
        private void NewAccount(C.NewAccount p)
        {
            if (Stage != GameStage.Login) return;

            SMain.Enqueue(SessionID + ", " + IPAddress + ", New account being created.");
            SMain.Envir.NewAccount(p, this);
        }
        private void ChangePassword(C.ChangePassword p)
        {
            if (Stage != GameStage.Login) return;

            SMain.Enqueue(SessionID + ", " + IPAddress + ", Password being changed.");
            SMain.Envir.ChangePassword(p, this);
        }
        private void Login(C.Login p)
        {
            if (Stage != GameStage.Login) return;

            SMain.Enqueue(SessionID + ", " + IPAddress + ", User logging in.");
            SMain.Envir.Login(p, this);
        }
        private void NewCharacter(C.NewCharacter p)
        {
            if (Stage != GameStage.Select) return;

            SMain.Envir.NewCharacter(p, this, Account.AdminAccount);
        }
        private void DeleteCharacter(C.DeleteCharacter p)
        {
            if (Stage != GameStage.Select) return;
            
            if (!Settings.AllowDeleteCharacter)
            {
                Enqueue(new S.DeleteCharacter { Result = 0 });
                return;
            }

            CharacterInfo temp = null;
            

            for (int i = 0; i < Account.Characters.Count; i++)
			{
			    if (Account.Characters[i].Index != p.CharacterIndex) continue;

			    temp = Account.Characters[i];
			    break;
			}

            if (temp == null)
            {
                Enqueue(new S.DeleteCharacter { Result = 1 });
                return;
            }

            temp.Deleted = true;
            temp.DeleteDate = SMain.Envir.Now;
            //SMain.Envir.RemoveRank(temp);
            Enqueue(new S.DeleteCharacterSuccess { CharacterIndex = temp.Index });
        }

        //刷新用户的账户信息
        public void RefreshUserGold()
        {
            if (Account != null)
            {
                Enqueue(new S.UserGold { Gold = Account.Gold, Credit=Account.Credit });
            }
        }
        //充值元宝
        private void RechargeCredit(C.RechargeCredit p)
        {
            if (Account == null)
            {
                return;
            }
            //创建支付订单
            PayOrder.CreateOrder(Account.Index,p.price,p.pay_type);
        }

        //客户端发送充值完成，服务器查询结果
        private void RechargeEnd(C.RechargeEnd p)
        {
            PayOrder.PayEnd(p.oid);
        }

        //客户端发起刷新背包请求
        private void RefreshInventory(C.RefreshInventory p)
        {
            Player.RefreshInventory();
        }

        //放入收集物品
        private void DepositItemCollect(C.DepositItemCollect p)
        {
            if (Stage != GameStage.Game) return;

            Player.DepositItemCollect(p.From, p.To);
        }
        //取回收集物品
        private void RetrieveItemCollect(C.RetrieveItemCollect p)
        {
            if (Stage != GameStage.Game) return;

            Player.RetrieveItemCollect(p.From, p.To);
        }

        //取消物品收集
        private void ItemCollectCancel(C.ItemCollectCancel p)
        {
            if (Stage != GameStage.Game) return;

            Player.ItemCollectCancel();
            //Player.RefreshInventory();
        }
        private void ConfirmItemCollect(C.ConfirmItemCollect p)
        {
            if (Stage != GameStage.Game) return;

            Player.ConfirmItemCollect(p.type);
        }

        private void MagicParameter(C.MagicParameter p)
        {
            if (Stage != GameStage.Game) return;

            Player.MagicParameter = p.Parameter;
        }

        private void MyMonsterOperation(C.MyMonsterOperation p)
        {
            if (Stage != GameStage.Game) return;

            Player.MyMonsterOperation(p.monidx, p.operation, p.parameter1, p.parameter2);
        }
        

        private void StartGame(C.StartGame p)
        {
            if (Stage != GameStage.Select) return;

            if (!Settings.AllowStartGame && (Account == null || (Account != null && !Account.AdminAccount)))
            {
                Enqueue(new S.StartGame { Result = 0 });
                return;
            }

            if (Account == null)
            {
                Enqueue(new S.StartGame { Result = 1 });
                return;
            }


            CharacterInfo info = null;

            for (int i = 0; i < Account.Characters.Count; i++)
            {
                if (Account.Characters[i].Index != p.CharacterIndex) continue;

                info = Account.Characters[i];
                break;
            }
            if (info == null)
            {
                Enqueue(new S.StartGame { Result = 2 });
                return;
            }

            if (info.Banned)
            {
                if (info.ExpiryDate > DateTime.Now)
                {
                    Enqueue(new S.StartGameBanned { Reason = info.BanReason, ExpiryDate = info.ExpiryDate });
                    return;
                }
                info.Banned = false;
            }
            info.BanReason = string.Empty;
            info.ExpiryDate = DateTime.MinValue;

            long delay = (long) (SMain.Envir.Now - info.LastDate).TotalMilliseconds;


            //if (delay < Settings.RelogDelay)
            //{
            //    Enqueue(new S.StartGameDelay { Milliseconds = Settings.RelogDelay - delay });
            //    return;
            //}

            Player = new PlayerObject(info, this);
            Player.StartGame();
        }

        //小退
        public void LogOut()
        {
            if (Stage != GameStage.Game) return;

            if (ServerConfig.exitGameType == ExitGameType.Normal&&SMain.Envir.Time < Player.LogTime)
            {
                Enqueue(new S.LogOutFailed());
                return;
            }

            Player.StopGame(23);

            Stage = GameStage.Select;
            Player = null;
            Enqueue(new S.LogOutSuccess { Characters = Account.GetSelectInfo()});
            //这里发送一次校验码，验证码
            if(Account.NextCheckTime > 0 && Account.NextCheckTime < SMain.Envir.Time + 1000 * 60 * 60)
            {
                SendCheckCode(1);
            }
        }

        private void Turn(C.Turn p)
        {
            if (Stage != GameStage.Game) return;

            if (Player.ActionTime > SMain.Envir.Time)
                _retryList.Enqueue(p);
            else
                Player.Turn(p.Direction);
        }
        private void Walk(C.Walk p)
        {
            if (Stage != GameStage.Game) return;

            if (Player.ActionTime > SMain.Envir.Time)
                _retryList.Enqueue(p);
            else
                Player.Walk(p.Direction);
        }
        private void Run(C.Run p)
        {
            if (Stage != GameStage.Game) return;
            
            if (Player.ActionTime > SMain.Envir.Time)
                _retryList.Enqueue(p);
            else
                Player.Run(p.Direction);
        }
        
        private void Chat(C.Chat p)
        {
            if (p.Message.Length > Globals.MaxChatLength)
            {
                SendDisconnect(2);
                return;
            }

            if (Stage != GameStage.Game) return;

            Player.Chat(p.Message);
        }

        private void MoveItem(C.MoveItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.MoveItem(p.Grid, p.From, p.To);
        }
        private void StoreItem(C.StoreItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.StoreItem(p.From, p.To);
        }

        private void DepositRefineItem(C.DepositRefineItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.DepositRefineItem(p.From, p.To);
        }

        private void RetrieveRefineItem(C.RetrieveRefineItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.RetrieveRefineItem(p.From, p.To);
        }

        private void RefineCancel(C.RefineCancel p)
        {
            if (Stage != GameStage.Game) return;

            Player.RefineCancel();
        }

        private void RefineItem(C.RefineItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.RefineItem(p.UniqueID);
        }

        private void CheckRefine(C.CheckRefine p)
        {
            if (Stage != GameStage.Game) return;

            Player.CheckRefine(p.UniqueID);
        }
        //打造结婚戒指
        private void ReplaceWedRing(C.ReplaceWedRing p)
        {
            if (Stage != GameStage.Game) return;

            Player.ReplaceWeddingRing(p.UniqueID);
        }

        private void DepositTradeItem(C.DepositTradeItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.DepositTradeItem(p.From, p.To);
        }
        
        private void RetrieveTradeItem(C.RetrieveTradeItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.RetrieveTradeItem(p.From, p.To);
        }
        private void TakeBackItem(C.TakeBackItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.TakeBackItem(p.From, p.To);
        }
        private void MergeItem(C.MergeItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.MergeItem(p.GridFrom, p.GridTo, p.IDFrom, p.IDTo);
        }
        private void EquipItem(C.EquipItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.EquipItem(p.Grid, p.UniqueID, p.To);
        }
        private void RemoveItem(C.RemoveItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.RemoveItem(p.Grid, p.UniqueID, p.To);
        }
        private void RemoveSlotItem(C.RemoveSlotItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.RemoveSlotItem(p.Grid, p.UniqueID, p.To, p.GridTo);
        }
        private void SplitItem(C.SplitItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.SplitItem(p.Grid, p.UniqueID, p.Count);
        }
        private void UseItem(C.UseItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.UseItem(p.UniqueID);
        }
        private void DropItem(C.DropItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.DropItem(p.UniqueID, p.Count);
        }
        private void DropGold(C.DropGold p)
        {
            if (Stage != GameStage.Game) return;

            Player.DropGold(p.Amount);
        }
        private void PickUp()
        {
            if (Stage != GameStage.Game) return;

            Player.PickUp();
        }
        private void Inspect(C.Inspect p)
        {
            if (Stage != GameStage.Game) return;

            if (p.Ranking)
                Player.Inspect((ulong)p.ObjectID);
            else
                Player.Inspect(p.ObjectID);
        }
        private void ChangeAMode(C.ChangeAMode p)
        {
            if (Stage != GameStage.Game) return;

            Player.AMode = p.Mode;

            Enqueue(new S.ChangeAMode {Mode = Player.AMode});
        }
        private void ChangePMode(C.ChangePMode p)
        {
            if (Stage != GameStage.Game) return;
            if (Player.Class != MirClass.Wizard && Player.Class != MirClass.Taoist && Player.Pets.Count == 0)
                return;

            Player.PMode = p.Mode;

            Enqueue(new S.ChangePMode { Mode = Player.PMode });
        }
        private void ChangeTrade(C.ChangeTrade p)
        {
            if (Stage != GameStage.Game) return;

            Player.AllowTrade = p.AllowTrade;
        }
        private void Attack(C.Attack p)
        {
            if (Stage != GameStage.Game) return;

            if (!Player.Dead && (Player.ActionTime > SMain.Envir.Time || Player.AttackTime > SMain.Envir.Time))
                _retryList.Enqueue(p);
            else
                Player.Attack(p.Direction, p.Spell);
        }
        private void RangeAttack(C.RangeAttack p)
        {
            if (Stage != GameStage.Game) return;

            if (!Player.Dead && (Player.ActionTime > SMain.Envir.Time || Player.AttackTime > SMain.Envir.Time))
                _retryList.Enqueue(p);
            else
                Player.RangeAttack(p.Direction, p.TargetLocation, p.TargetID);
        }
        private void Harvest(C.Harvest p)
        {
            if (Stage != GameStage.Game) return;

            if (!Player.Dead && Player.ActionTime > SMain.Envir.Time)
                _retryList.Enqueue(p);
            else
                Player.Harvest(p.Direction);
        }

        private void CallNPC(C.CallNPC p)
        {
            if (Stage != GameStage.Game) return;

            if (p.Key.Length > 30) //No NPC Key should be that long.
            {
                SendDisconnect(2);
                return;
            }

            if (p.ObjectID == Player.DefaultNPC.ObjectID && Player.NPCID == Player.DefaultNPC.ObjectID)
            {
                Player.CallDefaultNPC(p.ObjectID, p.Key);
                return;
            }

            Player.CallNPC(p.ObjectID, p.Key);
        }

        private void TalkMonsterNPC(C.TalkMonsterNPC p)
        {
            if (Stage != GameStage.Game) return;

            Player.TalkMonster(p.ObjectID);
        }

        private void BuyItem(C.BuyItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.BuyItem(p.ItemIndex, p.Count, p.Type);
        }
        private void CraftItem(C.CraftItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.CraftItem(p.UniqueID, p.Count, p.Slots);
        }
        private void SellItem(C.SellItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.SellItem(p.UniqueID, p.Count);
        }
        private void RepairItem(C.RepairItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.RepairItem(p.UniqueID);
        }
        private void BuyItemBack(C.BuyItemBack p)
        {
            if (Stage != GameStage.Game) return;

           // Player.BuyItemBack(p.UniqueID, p.Count);
        }
        private void SRepairItem(C.SRepairItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.RepairItem(p.UniqueID, true);
        }
        private void MagicKey(C.MagicKey p)
        {
            if (Stage != GameStage.Game) return;

            for (int i = 0; i < Player.Info.Magics.Count; i++)
            {
                UserMagic magic = Player.Info.Magics[i];
                if (magic.Spell != p.Spell)
                {
                    if (magic.Key == p.Key)
                        magic.Key = 0;
                    continue;
                }

                magic.Key = p.Key;
            }
        }
        private void Magic(C.Magic p)
        {
            if (Stage != GameStage.Game) return;
            //这个逻辑是否有问题？死了还可以放技能么？
            if (!Player.Dead && (Player.ActionTime > SMain.Envir.Time || Player.SpellTime > SMain.Envir.Time))
                _retryList.Enqueue(p);
            else
                Player.Magic(p.Spell, p.Direction, p.TargetID, p.Location);
        }

        private void SwitchGroup(C.SwitchGroup p)
        {
            if (Stage != GameStage.Game) return;

            Player.SwitchGroup(p.AllowGroup);
        }
        private void AddMember(C.AddMember p)
        {
            if (Stage != GameStage.Game) return;

            Player.AddMember(p.Name);
        }
        private void DelMember(C.DelMember p)
        {
            if (Stage != GameStage.Game) return;

            Player.DelMember(p.Name);
        }
        private void GroupInvite(C.GroupInvite p)
        {
            if (Stage != GameStage.Game) return;

            Player.GroupInvite(p.AcceptInvite);
        }

        private void TownRevive()
        {
            if (Stage != GameStage.Game) return;

            Player.TownRevive();
        }

        private void SpellToggle(C.SpellToggle p)
        {
            if (Stage != GameStage.Game) return;

            Player.SpellToggle(p.Spell, p.CanUse);
        }
        private void ConsignItem(C.ConsignItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.ConsignItem(p.UniqueID, p.GoldPrice,p.CreditPrice);
        }
        private void MarketSearch(C.MarketSearch p)
        {
            if (Stage != GameStage.Game) return;

            Player.MarketSearch(p.Match, p.itemtype,p.Page);
        }
     
        private void MarketBuy(C.MarketBuy p)
        {
            if (Stage != GameStage.Game) return;

            Player.MarketBuy(p.AuctionID, p.payType);
        }
        private void MarketGetBack(C.MarketGetBack p)
        {
            if (Stage != GameStage.Game) return;

            Player.MarketGetBack(p.AuctionID);
        }
        private void RequestUserName(C.RequestUserName p)
        {
            if (Stage != GameStage.Game) return;

            Player.RequestUserName(p.UserID);
        }
        private void RequestChatItem(C.RequestChatItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.RequestChatItem(p.ChatItemID);
        }
        private void EditGuildMember(C.EditGuildMember p)
        {
            if (Stage != GameStage.Game) return;
            Player.EditGuildMember(p.Name,p.RankName,p.RankIndex,p.ChangeType);
        }
        private void EditGuildNotice(C.EditGuildNotice p)
        {
            if (Stage != GameStage.Game) return;
            Player.EditGuildNotice(p.notice);
        }
        private void GuildInvite(C.GuildInvite p)
        {
            if (Stage != GameStage.Game) return;

            Player.GuildInvite(p.AcceptInvite);
        }
        private void RequestGuildInfo(C.RequestGuildInfo p)
        {
            if (Stage != GameStage.Game) return;
            Player.RequestGuildInfo(p.Type);
        }
        private void GuildNameReturn(C.GuildNameReturn p)
        {
            if (Stage != GameStage.Game) return;
            Player.GuildNameReturn(p.Name);
        }
        private void GuildStorageGoldChange(C.GuildStorageGoldChange p)
        {
            if (Stage != GameStage.Game) return;
            Player.GuildStorageGoldChange(p.Type, p.Amount);
        }
        private void GuildStorageItemChange(C.GuildStorageItemChange p)
        {
            if (Stage != GameStage.Game) return;
            Player.GuildStorageItemChange(p.Type, p.From, p.To);
        }
        private void GuildWarReturn(C.GuildWarReturn p)
        {
            if (Stage != GameStage.Game) return;
            Player.GuildWarReturn(p.Name);
        }


        private void MarriageRequest(C.MarriageRequest p)
        {
            if (Stage != GameStage.Game) return;

            Player.MarriageRequest();
        }

        private void MarriageReply(C.MarriageReply p)
        {
            if (Stage != GameStage.Game) return;

            Player.MarriageReply(p.AcceptInvite);
        }

        private void ChangeMarriage(C.ChangeMarriage p)
        {
            if (Stage != GameStage.Game) return;

            if (Player.Info.Married == 0)
            {
                Player.AllowMarriage = !Player.AllowMarriage;
                if (Player.AllowMarriage)
                    Player.ReceiveChat("你现在允许结婚请求了.", ChatType.Hint);
                else
                    Player.ReceiveChat("你现在拒绝结婚请求了.", ChatType.Hint);
            }
            else
            {
                Player.AllowLoverRecall = !Player.AllowLoverRecall;
                if (Player.AllowLoverRecall)
                    Player.ReceiveChat("你现在允许夫妻传送.", ChatType.Hint);
                else
                    Player.ReceiveChat("你现在拒绝夫妻传送.", ChatType.Hint);
            }
        }

        private void DivorceRequest(C.DivorceRequest p)
        {
            if (Stage != GameStage.Game) return;

            Player.DivorceRequest();
        }

        private void DivorceReply(C.DivorceReply p)
        {
            if (Stage != GameStage.Game) return;

            Player.DivorceReply(p.AcceptInvite);
        }

        private void AddMentor(C.AddMentor p)
        {
            if (Stage != GameStage.Game) return;

            Player.AddMentor(p.Name);
        }

        private void MentorReply(C.MentorReply p)
        {
            if (Stage != GameStage.Game) return;

            Player.MentorReply(p.AcceptInvite);
        }

        private void AllowMentor(C.AllowMentor p)
        {
            if (Stage != GameStage.Game) return;

                Player.AllowMentor = !Player.AllowMentor;
                if (Player.AllowMentor)
                    Player.ReceiveChat("你现在允许拜师.", ChatType.Hint);
                else
                    Player.ReceiveChat("你现在拒绝拜师.", ChatType.Hint);
        }

        private void CancelMentor(C.CancelMentor p)
        {
            if (Stage != GameStage.Game) return;

            Player.MentorBreak(true);
        }

        private void TradeRequest(C.TradeRequest p)
        {
            if (Stage != GameStage.Game) return;

            Player.TradeRequest();
        }
        private void TradeGold(C.TradeGold p)
        {
            if (Stage != GameStage.Game) return;

            Player.TradeGold(p.Amount);
        }
        private void TradeReply(C.TradeReply p)
        {
            if (Stage != GameStage.Game) return;

            Player.TradeReply(p.AcceptInvite);
        }
        private void TradeConfirm(C.TradeConfirm p)
        {
            if (Stage != GameStage.Game) return;

            Player.TradeConfirm(p.Locked);
        }
        private void TradeCancel(C.TradeCancel p)
        {
            if (Stage != GameStage.Game) return;

            Player.TradeCancel();
        }
        private void EquipSlotItem(C.EquipSlotItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.EquipSlotItem(p.Grid, p.UniqueID, p.To, p.GridTo);
        }

        private void FishingCast(C.FishingCast p)
        {
            if (Stage != GameStage.Game) return;

            Player.FishingCast(p.CastOut, true);
        }

        private void FishingChangeAutocast(C.FishingChangeAutocast p)
        {
            if (Stage != GameStage.Game) return;

            Player.FishingChangeAutocast(p.AutoCast);
        }

        private void AcceptQuest(C.AcceptQuest p)
        {
            if (Stage != GameStage.Game) return;

            Player.AcceptQuest(p.QuestIndex); //p.NPCIndex,
        }

        private void FinishQuest(C.FinishQuest p)
        {
            if (Stage != GameStage.Game) return;

            Player.FinishQuest(p.QuestIndex, p.SelectedItemIndex);
        }

        private void AbandonQuest(C.AbandonQuest p)
        {
            if (Stage != GameStage.Game) return;

            Player.AbandonQuest(p.QuestIndex);
        }

        private void ShareQuest(C.ShareQuest p)
        {
            if (Stage != GameStage.Game) return;

            Player.ShareQuest(p.QuestIndex);
        }

        private void AcceptReincarnation()
        {
            if (Stage != GameStage.Game) return;

            if (Player.ReincarnationHost != null && Player.ReincarnationHost.ReincarnationReady)
            {
                Player.Revive((uint)Player.MaxHP / 2, true);
                Player.ReincarnationHost = null;
                return;
            }

            Player.ReceiveChat("复活失败", ChatType.System);
        }

        private void CancelReincarnation()
        {
            if (Stage != GameStage.Game) return;
            Player.ReincarnationExpireTime = SMain.Envir.Time;

        }

        private void CombineItem(C.CombineItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.CombineItem(p.IDFrom, p.IDTo);
        }

        private void SetConcentration(C.SetConcentration p)
        {
            if (Stage != GameStage.Game) return;

            Player.ConcentrateInterrupted = p.Interrupted;
        }

        private void Awakening(C.Awakening p)
        {
            if (Stage != GameStage.Game) return;

            Player.Awakening(p.UniqueID, p.Type);
        }

        private void AwakeningNeedMaterials(C.AwakeningNeedMaterials p)
        {
            if (Stage != GameStage.Game) return;

            Player.AwakeningNeedMaterials(p.UniqueID, p.Type);
        }

        private void DisassembleItem(C.DisassembleItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.DisassembleItem(p.UniqueID);
        }

        private void DowngradeAwakening(C.DowngradeAwakening p)
        {
            if (Stage != GameStage.Game) return;

            Player.DowngradeAwakening(p.UniqueID);
        }

        private void ResetAddedItem(C.ResetAddedItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.ResetAddedItem(p.UniqueID);
        }

        public void SendMail(C.SendMail p)
        {
            if (Stage != GameStage.Game) return;

            if (p.Gold > 0 || p.ItemsIdx.Length > 0)
            {
                Player.SendMail(p.Name, p.Message, p.Gold, p.ItemsIdx, p.Stamped);
            }
            else
            {
                Player.SendMail(p.Name, p.Message);
            }
        }

        public void ReadMail(C.ReadMail p)
        {
            if (Stage != GameStage.Game) return;

            Player.ReadMail(p.MailID);
        }

        public void CollectParcel(C.CollectParcel p)
        {
            if (Stage != GameStage.Game) return;

            Player.CollectMail(p.MailID);
        }

        public void DeleteMail(C.DeleteMail p)
        {
            if (Stage != GameStage.Game) return;

            Player.DeleteMail(p.MailID);
        }

        public void LockMail(C.LockMail p)
        {
            if (Stage != GameStage.Game) return;

            Player.LockMail(p.MailID, p.Lock);
        }

        public void MailCost(C.MailCost p)
        {
            if (Stage != GameStage.Game) return;

            uint cost = Player.GetMailCost(p.ItemsIdx, p.Gold, p.Stamped);

            Enqueue(new S.MailCost { Cost = cost });
        }

        private void UpdateIntelligentCreature(C.UpdateIntelligentCreature p)//IntelligentCreature
        {
            if (Stage != GameStage.Game) return;

            ClientIntelligentCreature petUpdate = p.Creature;
            if (petUpdate == null) return;

            if (p.ReleaseMe)
            {
                Player.ReleaseIntelligentCreature(petUpdate.PetType);
                return;
            }
            else if (p.SummonMe)
            {
                Player.SummonIntelligentCreature(petUpdate.PetType);
                return;
            }
            else if (p.UnSummonMe)
            {
                Player.UnSummonIntelligentCreature(petUpdate.PetType);
                return;
            }
            else
            {
                //Update the creature info
                for (int i = 0; i < Player.Info.IntelligentCreatures.Count; i++)
                {
                    if (Player.SummonedCreatureType != petUpdate.PetType && Player.Info.IntelligentCreatures[i].PetType == petUpdate.PetType)
                    {
                        if (petUpdate.CustomName.Length <= 12)
                            Player.Info.IntelligentCreatures[i].CustomName = petUpdate.CustomName;
                        Player.Info.IntelligentCreatures[i].SlotIndex = petUpdate.SlotIndex;
                        Player.Info.IntelligentCreatures[i].Filter = petUpdate.Filter;
                        Player.Info.IntelligentCreatures[i].petMode = petUpdate.petMode;
                    }
                    else continue;
                }

                if (Player.CreatureSummoned)
                {
                    if (Player.SummonedCreatureType == petUpdate.PetType)
                        Player.UpdateSummonedCreature(petUpdate.PetType);
                }
            }
        }

        private void IntelligentCreaturePickup(C.IntelligentCreaturePickup p)//IntelligentCreature
        {
            if (Stage != GameStage.Game) return;

            Player.IntelligentCreaturePickup(p.MouseMode, p.Location);
        }

        private void AddFriend(C.AddFriend p)
        {
            if (Stage != GameStage.Game) return;

            Player.AddFriend(p.Name, p.Blocked);
        }

        private void RemoveFriend(C.RemoveFriend p)
        {
            if (Stage != GameStage.Game) return;

            Player.RemoveFriend(p.CharacterIndex);
        }

        private void AddMemo(C.AddMemo p)
        {
            if (Stage != GameStage.Game) return;

            Player.AddMemo(p.CharacterIndex, p.Memo);
        }
        private void GuildBuffUpdate(C.GuildBuffUpdate p)
        {
            if (Stage != GameStage.Game) return;
            Player.GuildBuffUpdate(p.Action,p.Id);
        }
        private void GameshopBuy(C.GameshopBuy p)
        {
            if (Stage != GameStage.Game) return;
            Player.GameshopBuy(p.GIndex,p.payType, p.Quantity);
        }

        private void NPCConfirmInput(C.NPCConfirmInput p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCInputStr = p.Value;

            Player.CallNPC(Player.NPCID, p.PageName);
        }

        public List<byte[]> Image = new List<byte[]>();
        
        private void ReportIssue(C.ReportIssue p)
        {
            if (Stage != GameStage.Game) return;

            return;
            /**
            Image.Add(p.Image);
            if (p.ImageChunk >= p.ImageSize)
            {
                System.Drawing.Image image = Functions.ByteArrayToImage(Functions.CombineArray(Image));
                image.Save("Reported-" + Player.Name + "-" + DateTime.Now.ToString("yyMMddHHmmss") + ".jpg");
                Image.Clear();
            }
            **/
        }
        private void GetRanking(C.GetRanking p)
        {
            if (Stage != GameStage.Game) return;
            Player.GetRanking(p.RankIndex,p.RankType);
        }

        private void Opendoor(C.Opendoor p)
        {
            if (Stage != GameStage.Game) return;
            Player.Opendoor(p.DoorIndex);
        }

        private void GetRentedItems()
        {
            if (Stage != GameStage.Game)
                return;

            Player.GetRentedItems();
        }

        private void ItemRentalRequest()
        {
            if (Stage != GameStage.Game)
                return;

            Player.ItemRentalRequest();
        }

        private void ItemRentalFee(C.ItemRentalFee p)
        {
            if (Stage != GameStage.Game)
                return;

            Player.SetItemRentalFee(p.Amount);
        }

        private void ItemRentalPeriod(C.ItemRentalPeriod p)
        {
            if (Stage != GameStage.Game)
                return;

            Player.SetItemRentalPeriodLength(p.Days);
        }

        private void DepositRentalItem(C.DepositRentalItem p)
        {
            if (Stage != GameStage.Game)
                return;

            Player.DepositRentalItem(p.From, p.To);
        }

        private void RetrieveRentalItem(C.RetrieveRentalItem p)
        {
            if (Stage != GameStage.Game)
                return;

            Player.RetrieveRentalItem(p.From, p.To);
        }

        private void CancelItemRental()
        {
            if (Stage != GameStage.Game)
                return;

            Player.CancelItemRental();
        }

        private void ItemRentalLockFee()
        {
            if (Stage != GameStage.Game)
                return;

            Player.ItemRentalLockFee();
        }

        private void ItemRentalLockItem()
        {
            if (Stage != GameStage.Game)
                return;

            Player.ItemRentalLockItem();
        }

        private void ConfirmItemRental()
        {
            if (Stage != GameStage.Game)
                return;

            Player.ConfirmItemRental();
        }
    }
}
