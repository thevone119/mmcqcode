using System.Drawing;
using Server.MirObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Server.MirEnvir;
using System.Data.SQLite;
using System.Data.Common;

namespace Server.MirDatabase
{
    //任务信息，定义各种任务
    public class QuestInfo
    {
        public int Index;

        public uint NpcIndex;
        //public NPCInfo NpcInfo;

        //接收任务的NPC的ID
        public int AcceptNpcIdx;
        //提交任务的NPC的ID
        public int SubmitNpcIdx;

        public uint _finishNpcIndex;

        public uint FinishNpcIndex
        {
            get { return _finishNpcIndex == 0 ? NpcIndex : _finishNpcIndex; }
            set { _finishNpcIndex = value; }
        }

        public string 
            Name = string.Empty, 
            Group = string.Empty, 
            FileName = string.Empty,
            FileText = string.Empty,
            GotoMessage = string.Empty, 
            KillMessage = string.Empty, 
            ItemMessage = string.Empty,
            FlagMessage = string.Empty;

        //描述（对话框描述）
        public List<string> Description = new List<string>();
        //任务描述
        public List<string> TaskDescription = new List<string>(); 
        //任务完成描述
        public List<string> CompletionDescription = new List<string>(); 

        //任务的最小等级，最大等级
        public int RequiredMinLevel, RequiredMaxLevel;

        //RequiredQuest：之前的任务，就是之前是否完成了某个任务，才可以做这个任务，如果之前没有做这个任务，那么就不能做当前的人任务
        //依赖任务，要求的任务
        public int RequiredQuest;
        public RequiredClass RequiredClass = RequiredClass.None;

        public QuestType Type;

        //托运物品
        public List<QuestItemTask> CarryItems = new List<QuestItemTask>(); 
        //杀怪任务
        public List<QuestKillTask> KillTasks = new List<QuestKillTask>();
        //获得物品任务
        public List<QuestItemTask> ItemTasks = new List<QuestItemTask>();
        //任务标记
        public List<QuestFlagTask> FlagTasks = new List<QuestFlagTask>();

        public uint GoldReward;//金币奖励
        public uint ExpReward;//经验奖励
        public uint CreditReward;//元宝奖励



        //固定奖励物品
        public List<QuestItemReward> FixedRewards = new List<QuestItemReward>();
        //选择奖励物品（多选1）
        public List<QuestItemReward> SelectRewards = new List<QuestItemReward>();

        private Regex _regexMessage = new Regex("\"([^\"]*)\"");


        public QuestInfo() { }

        /// <summary>
        /// 加载所有数据
        /// </summary>
        /// <returns></returns>
        public static List<QuestInfo> loadAll()
        {
            List<QuestInfo> list = new List<QuestInfo>();
            DbDataReader read = MirConfigDB.ExecuteReader("select * from QuestInfo where isdel=0");
            if (!Settings.openTaskQuest)
            {
                return list;
            }
            while (read.Read())
            {
                QuestInfo obj = new QuestInfo();
                if (read.IsDBNull(read.GetOrdinal("Name")))
                {
                    continue;
                }
                obj.Name = read.GetString(read.GetOrdinal("Name"));
                if (obj.Name == null)
                {
                    continue;
                }
                obj.Index = read.GetInt32(read.GetOrdinal("Idx"));

                obj.Group = read.GetString(read.GetOrdinal("GroupName"));
                obj.FileName = read.GetString(read.GetOrdinal("FileName"));
                
                obj.RequiredMinLevel = read.GetInt32(read.GetOrdinal("RequiredMinLevel"));
                obj.RequiredMaxLevel = read.GetInt32(read.GetOrdinal("RequiredMaxLevel"));
                obj.AcceptNpcIdx = read.GetInt32(read.GetOrdinal("AcceptNpcIdx"));
                obj.SubmitNpcIdx = read.GetInt32(read.GetOrdinal("SubmitNpcIdx"));


                if (obj.RequiredMaxLevel == 0) obj.RequiredMaxLevel = ushort.MaxValue;

                obj.RequiredQuest = read.GetInt32(read.GetOrdinal("RequiredQuest"));
                obj.RequiredClass = (RequiredClass)read.GetInt32(read.GetOrdinal("RequiredClass"));
                obj.Type = (QuestType)read.GetInt32(read.GetOrdinal("Type"));

                obj.GotoMessage = read.GetString(read.GetOrdinal("GotoMessage"));
                obj.KillMessage = read.GetString(read.GetOrdinal("KillMessage"));
                obj.ItemMessage = read.GetString(read.GetOrdinal("ItemMessage"));
                obj.FlagMessage = read.GetString(read.GetOrdinal("FlagMessage"));
                obj.FileText = read.GetString(read.GetOrdinal("FileText"));
                //加载脚本
                obj.LoadInfo();
                
                DBObjectUtils.updateObjState(obj, obj.Index);
                list.Add(obj);
            }
            return list;
        }


        public QuestInfo(BinaryReader reader)
        {
            Index = reader.ReadInt32();
            Name = reader.ReadString();
            Group = reader.ReadString();
            FileName = reader.ReadString();
            RequiredMinLevel = reader.ReadInt32();

            if (Envir.LoadVersion >= 38)
            {
                RequiredMaxLevel = reader.ReadInt32();
                if (RequiredMaxLevel == 0) RequiredMaxLevel = ushort.MaxValue;
            }

            RequiredQuest = reader.ReadInt32();
            RequiredClass = (RequiredClass)reader.ReadByte();
            Type = (QuestType)reader.ReadByte();
            GotoMessage = reader.ReadString();
            KillMessage = reader.ReadString();
            ItemMessage = reader.ReadString();
            if(Envir.LoadVersion >= 37) FlagMessage = reader.ReadString();

            LoadInfo();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(Name);
            writer.Write(Group);
            writer.Write(FileName);
            writer.Write(RequiredMinLevel);
            writer.Write(RequiredMaxLevel);
            writer.Write(RequiredQuest);
            writer.Write((byte)RequiredClass);
            writer.Write((byte)Type);
            writer.Write(GotoMessage);
            writer.Write(KillMessage);
            writer.Write(ItemMessage);
            writer.Write(FlagMessage);
            SaveDB();
        }

        //保存到数据库
        public void SaveDB()
        {
            byte state = DBObjectUtils.ObjState(this, Index);
            if (state == 0)//没有改变
            {
                return;
            }
            List<SQLiteParameter> lp = new List<SQLiteParameter>();
            lp.Add(new SQLiteParameter("Name", Name));
            lp.Add(new SQLiteParameter("GroupName", Group));
            lp.Add(new SQLiteParameter("FileName", FileName));
            lp.Add(new SQLiteParameter("RequiredMinLevel", RequiredMinLevel));
            lp.Add(new SQLiteParameter("RequiredMaxLevel", RequiredMaxLevel));

            lp.Add(new SQLiteParameter("RequiredQuest", RequiredQuest));
            lp.Add(new SQLiteParameter("RequiredClass", RequiredClass));
            lp.Add(new SQLiteParameter("Type", Type));
            lp.Add(new SQLiteParameter("GotoMessage", GotoMessage));

            lp.Add(new SQLiteParameter("KillMessage", KillMessage));
            lp.Add(new SQLiteParameter("ItemMessage", ItemMessage));
            lp.Add(new SQLiteParameter("FlagMessage", FlagMessage));

            //新增
            if (state == 1)
            {
                if (Index > 0)
                {
                    lp.Add(new SQLiteParameter("Idx", Index));
                }
                string sql = "insert into QuestInfo" + SQLiteHelper.createInsertSql(lp.ToArray()); ;
                MirConfigDB.Execute(sql, lp.ToArray());
            }
            //修改
            if (state == 2)
            {
                string sql = "update QuestInfo set " + SQLiteHelper.createUpdateSql(lp.ToArray()) + " where Idx=@Idx";
                lp.Add(new SQLiteParameter("Idx", Index));
                MirConfigDB.Execute(sql, lp.ToArray());
            }
            DBObjectUtils.updateObjState(this, Index);
            
        }

        //任务脚本
        public void LoadInfo(bool clear = false)
        {
            List<string> lines = null;
            if (clear) ClearInfo();
            if(FileText!=null&& FileText.Length > 10)
            {
                lines = FileText.Split(new string[] { "\n" }, StringSplitOptions.None).ToList();
                ParseFile(lines);
                return;
            }

            if (!Directory.Exists(Settings.QuestPath)) return;

            string fileName = Path.Combine(Settings.QuestPath, FileName + ".txt");

            if (File.Exists(fileName))
            {
                lines = File.ReadAllLines(fileName).ToList();

                ParseFile(lines);
            }
            else
                SMain.Enqueue(string.Format("File Not Found: {0}, Quest: {1}", fileName, Name));
        }

        public void ClearInfo()
        {
            Description.Clear();
            KillTasks = new List<QuestKillTask>();
            ItemTasks = new List<QuestItemTask>();
            FlagTasks = new List<QuestFlagTask>();
            FixedRewards = new List<QuestItemReward>();
            SelectRewards = new List<QuestItemReward>();
            ExpReward = 0;
            GoldReward = 0;
            CreditReward = 0;
        }

        public void ParseFile(List<string> lines)
        {
            const string
                descriptionCollectKey = "[@DESCRIPTION]",
                descriptionTaskKey = "[@TASKDESCRIPTION]",
                descriptionCompletionKey = "[@COMPLETION]",
                carryItemsKey = "[@CARRYITEMS]",
                killTasksKey = "[@KILLTASKS]",
                itemTasksKey = "[@ITEMTASKS]",
                flagTasksKey = "[@FLAGTASKS]",
                fixedRewardsKey = "[@FIXEDREWARDS]",
                selectRewardsKey = "[@SELECTREWARDS]",
                expRewardKey = "[@EXPREWARD]",
                goldRewardKey = "[@GOLDREWARD]",
                creditRewardKey = "[@CREDITREWARD]";

            List<string> headers = new List<string> 
            { 
                descriptionCollectKey, descriptionTaskKey, descriptionCompletionKey,
                carryItemsKey, killTasksKey, itemTasksKey, flagTasksKey,
                fixedRewardsKey, selectRewardsKey, expRewardKey, goldRewardKey, creditRewardKey
            };

            int currentHeader = 0;

            while (currentHeader < headers.Count)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    string line = lines[i].ToUpper();

                    if (line != headers[currentHeader].ToUpper()) continue;

                    for (int j = i + 1; j < lines.Count; j++)
                    {
                        string innerLine = lines[j];

                        if (innerLine.StartsWith("[")) break;
                        if (string.IsNullOrEmpty(lines[j])) continue;

                        switch (line)
                        {
                            case descriptionCollectKey:
                                Description.Add(innerLine);
                                break;
                            case descriptionTaskKey:
                                TaskDescription.Add(innerLine);
                                break;
                            case descriptionCompletionKey:
                                CompletionDescription.Add(innerLine);
                                break;
                            case carryItemsKey:
                                QuestItemTask t = ParseItem(innerLine);
                                if (t != null) CarryItems.Add(t);
                                break;
                            case killTasksKey:
                                QuestKillTask t1 = ParseKill(innerLine);
                                if(t1 != null) KillTasks.Add(t1);
                                break;
                            case itemTasksKey:
                                QuestItemTask t2 = ParseItem(innerLine);
                                if (t2 != null) ItemTasks.Add(t2);
                                break;
                            case flagTasksKey:
                                QuestFlagTask t3 = ParseFlag(innerLine);
                                if (t3 != null) FlagTasks.Add(t3);
                                break;
                            case fixedRewardsKey:
                                {
                                    ParseReward(FixedRewards, innerLine);
                                    break;
                                }
                            case selectRewardsKey:
                                {
                                    ParseReward(SelectRewards, innerLine);
                                    break;
                                }
                            case expRewardKey:
                                uint.TryParse(innerLine, out ExpReward);
                                break;
                            case goldRewardKey:
                                uint.TryParse(innerLine, out GoldReward);
                                break;
                            case creditRewardKey:
                                uint.TryParse(innerLine, out CreditReward);
                                break;
                        }
                    }
                }

                currentHeader++;
            }
        }

        public void ParseReward(List<QuestItemReward> list, string line)
        {
            if (line.Length < 1) return;

            string[] split = line.Split(' ');
            uint count = 1;

            if (split.Length > 1) uint.TryParse(split[1], out count);

            ItemInfo mInfo = ItemInfo.getItem(split[0]);

            if (mInfo == null)
            {
                mInfo = ItemInfo.getItem(split[0] + "(M)");
                if (mInfo != null) list.Add(new QuestItemReward() { Item = mInfo, Count = count });

                mInfo = ItemInfo.getItem(split[0] + "(F)");
                if (mInfo != null) list.Add(new QuestItemReward() { Item = mInfo, Count = count });
            }
            else
            {
                list.Add(new QuestItemReward() { Item = mInfo, Count = count });
            }
        }

        public QuestKillTask ParseKill(string line)
        {
            if (line.Length < 1) return null;

            string[] split = line.Split(' ');
            int count = 1;
            string message = "";

            MonsterInfo mInfo = SMain.Envir.GetMonsterInfo(split[0]);
            if (split.Length > 1) int.TryParse(split[1], out count);

            var match = _regexMessage.Match(line);
            if (match.Success)
            {
                message = match.Groups[1].Captures[0].Value;
            }

            return mInfo == null ? null : new QuestKillTask() { Monster = mInfo, Count = count, Message = message };
        }

        public QuestItemTask ParseItem(string line)
        {
            if (line.Length < 1) return null;

            string[] split = line.Split(' ');
            uint count = 1;
            string message = "";

            ItemInfo mInfo = ItemInfo.getItem(split[0]);
            if (split.Length > 1) uint.TryParse(split[1], out count);

            var match = _regexMessage.Match(line);
            if (match.Success)
            {
                message = match.Groups[1].Captures[0].Value;
            }
            //if (mInfo.StackSize <= 1)
            //{
            //    //recursively add item if cant stack???
            //}

            return mInfo == null ? null : new QuestItemTask { Item = mInfo, Count = count, Message = message };
        }

        public QuestFlagTask ParseFlag(string line)
        {
            if (line.Length < 1) return null;

            string[] split = line.Split(' ');

            int number = -1;
            string message = "";

            int.TryParse(split[0], out number);

            if (number < 0 || number > Globals.FlagIndexCount - 1000) return null;

            var match = _regexMessage.Match(line);
            if (match.Success)
            {
                message = match.Groups[1].Captures[0].Value;
            }

            return new QuestFlagTask { Number = number, Message = message };
        }

        public bool CanAccept(PlayerObject player)
        {
            if (RequiredMinLevel > player.Level || RequiredMaxLevel < player.Level)
                return false;

            if (RequiredQuest > 0 && !player.CompletedQuests.Contains(RequiredQuest))
                return false;

            switch (player.Class)
            {
                case MirClass.Warrior:
                    if (!RequiredClass.HasFlag(RequiredClass.Warrior))
                        return false;
                    break;
                case MirClass.Wizard:
                    if (!RequiredClass.HasFlag(RequiredClass.Wizard))
                        return false;
                    break;
                case MirClass.Taoist:
                    if (!RequiredClass.HasFlag(RequiredClass.Taoist))
                        return false;
                    break;
                case MirClass.Assassin:
                    if (!RequiredClass.HasFlag(RequiredClass.Assassin))
                        return false;
                    break;
                case MirClass.Archer:
                    if (!RequiredClass.HasFlag(RequiredClass.Archer))
                        return false;
                    break;
            }

            return true;
        }

        public ClientQuestInfo CreateClientQuestInfo()
        {
            return new ClientQuestInfo
            {
                Index = Index,
                NPCIndex = NpcIndex,
                FinishNPCIndex = FinishNpcIndex,
                Name = Name,
                Group = Group,
                Description = Description,
                TaskDescription = TaskDescription,
                CompletionDescription = CompletionDescription,
                MinLevelNeeded = RequiredMinLevel,
                MaxLevelNeeded = RequiredMaxLevel,
                ClassNeeded = RequiredClass,
                QuestNeeded = RequiredQuest,
                Type = Type,
                RewardGold = GoldReward,
                RewardExp = ExpReward,
                RewardCredit = CreditReward,
                RewardsFixedItem = FixedRewards,
                RewardsSelectItem = SelectRewards
            };
        }

        public static void FromText(string text)
        {
            string[] data = text.Split(new[] { ',' });

            if (data.Length < 10) return;

            QuestInfo info = new QuestInfo();

            info.Name = data[0];
            info.Group = data[1];

            byte temp;

            byte.TryParse(data[2], out temp);

            info.Type = (QuestType)temp;

            info.FileName = data[3];
            info.GotoMessage = data[4];
            info.KillMessage = data[5];
            info.ItemMessage = data[6];
            info.FlagMessage = data[7];

            int.TryParse(data[8], out info.RequiredMinLevel);
            int.TryParse(data[9], out info.RequiredMaxLevel);
            int.TryParse(data[10], out info.RequiredQuest);

            byte.TryParse(data[11], out temp);

            info.RequiredClass = (RequiredClass)temp;

            info.Index = (int)DBObjectUtils.getObjNextId(info);
            SMain.EditEnvir.QuestInfoList.Add(info);
        }

        public string ToText()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                Name, Group, (byte)Type, FileName, GotoMessage, KillMessage, ItemMessage, FlagMessage, RequiredMinLevel, RequiredMaxLevel, RequiredQuest, (byte)RequiredClass);
        }

        public override string ToString()
        {
            return string.Format("{0}:   {1}", Index, Name);
        }
    }

    public class QuestKillTask
    {
        public MonsterInfo Monster;
        public int Count;
        public string Message;
    }

    public class QuestItemTask
    {
        public ItemInfo Item;
        public uint Count;
        public string Message;
    }

    public class QuestFlagTask
    {
        public int Number;
        public string Message;
    }
}
