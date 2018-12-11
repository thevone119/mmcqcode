using Server.MirDatabase;
using Server.MirEnvir;
using Server.MirObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using S = ServerPackets;

namespace Server.MirObjects
{
    /// <summary>
    /// NPC对象
    /// </summary>
    public sealed class NPCObject : MapObject
    {

        public override ObjectType Race
        {
            get { return ObjectType.Merchant; }
        }

        public const string
            MainKey = "[@MAIN]",//主函数
            BuyKey = "[@BUY]",//买
            SellKey = "[@SELL]",//卖
            BuySellKey = "[@BUYSELL]",
            RepairKey = "[@REPAIR]",//修理
            SRepairKey = "[@SREPAIR]",//特殊修理
            RefineKey = "[@REFINE]",//精炼，升级吧
            RefineCheckKey = "[@REFINECHECK]",//精炼,检测
            RefineCollectKey = "[@REFINECOLLECT]",//精炼，收集
            ReplaceWedRingKey = "[@REPLACEWEDDINGRING]",
            BuyBackKey = "[@BUYBACK]",//回购
            StorageKey = "[@STORAGE]",//存储物品
            ConsignKey = "[@CONSIGN]",//寄售
            MarketKey = "[@MARKET]",//市场
            ConsignmentsKey = "[@CONSIGNMENT]",//寄售
            CraftKey = "[@CRAFT]",//工艺

            TradeKey = "[TRADE]",//交易
            RecipeKey = "[RECIPE]",//食谱
            TypeKey = "[TYPES]",//
            QuestKey = "[QUESTS]",//任务

            GuildCreateKey = "[@CREATEGUILD]",//创建行会
            RequestWarKey = "[@REQUESTWAR]",//请求战争，发起行会战
            SendParcelKey = "[@SENDPARCEL]",//发送包裹？
            CollectParcelKey = "[@COLLECTPARCEL]",//接受包裹
            AwakeningKey = "[@AWAKENING]",//觉醒
            DisassembleKey = "[@DISASSEMBLE]",//拆解
            DowngradeKey = "[@DOWNGRADE]",//降级
            ResetKey = "[@RESET]",//重置
            PearlBuyKey = "[@PEARLBUY]",//珍珠购买
            BuyUsedKey = "[@BUYUSED]";//买


        //public static Regex Regex = new Regex(@"[^\{\}]<.*?/(.*?)>");
        public static Regex Regex = new Regex(@"<.*?/(\@.*?)>");
        public NPCInfo Info;
        private const long TurnDelay = 10000;
        public long TurnTime, UsedGoodsTime, VisTime;
        public bool NeedSave;
        public bool Visible = true;
        public string NPCName;
        //NPC包含的物品分为3类，1是食材，2是用户物品，3是回购物品（回购即别人卖出去的，或者叫二手的）
        public List<UserItem> Goods = new List<UserItem>();
        public List<UserItem> UsedGoods = new List<UserItem>();
        public Dictionary<string, List<UserItem>> BuyBack = new Dictionary<string, List<UserItem>>();
        //物品分类
        public List<ItemType> Types = new List<ItemType>();
        //物品的
        public List<NPCPage> NPCSections = new List<NPCPage>();
        public List<QuestInfo> Quests = new List<QuestInfo>();
        public List<RecipeInfo> CraftGoods = new List<RecipeInfo>();

        public Dictionary<ulong, bool> VisibleLog = new Dictionary<ulong, bool>();
        //NPC分页，一般都是一页
        public List<NPCPage> NPCPages = new List<NPCPage>();
        //攻城战争
        public ConquestObject Conq;

        public float PriceRate(PlayerObject player, bool baseRate = false)
        {
            if (Conq == null || baseRate) return Info.Rate / 100F;

            if (player.MyGuild != null && player.MyGuild.Guildindex == Conq.Owner)
                return Info.Rate / 100F;
            else
                return (((Info.Rate / 100F) * Conq.npcRate) + Info.Rate) / 100F;
        }

        public NPCObject(NPCInfo info)
        {
            Info = info;
            NameColour = Color.Lime;

            if (!Info.IsDefault)
            {
                Direction = (MirDirection)RandomUtils.Next(3);
                TurnTime = Envir.Time + RandomUtils.Next(100);

                Spawned();
            }

            LoadInfo();
            LoadGoods();
        }
        //加载配置
        public void LoadInfo(bool clear = false)
        {
            if (clear) ClearInfo();

            if (!Directory.Exists(Settings.NPCPath)) return;

            string fileName = Path.Combine(Settings.NPCPath, Info.FileName + ".txt");

            if (File.Exists(fileName))
            {
                List<string> lines = File.ReadAllLines(fileName, EncodingType.GetType(fileName)).ToList();

                lines = ParseInsert(lines);
                lines = ParseInclude(lines);
                //加入双井号代表注解
                lines.RemoveAll(str => str.ToUpper().StartsWith("##"));
                if (Info.IsDefault)
                    ParseDefault(lines);
                else
                    ParseScript(lines);
            }
            else
                SMain.Enqueue(string.Format("File Not Found: {0}, NPC: {1}", Info.FileName, Info.Name));
        }
        public void ClearInfo()
        {
            Goods = new List<UserItem>();
            Types = new List<ItemType>();
            NPCPages = new List<NPCPage>();
            CraftGoods = new List<RecipeInfo>();

            if (Info.IsDefault)
            {
                SMain.Envir.CustomCommands.Clear();
            }
        }
        public void LoadGoods()
        {
            string path = Settings.GoodsPath + Info.Index.ToString() + ".msd";

            if (!File.Exists(path)) return;

            using (FileStream stream = File.OpenRead(path))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    int version = reader.ReadInt32();
                    int count = version;
                    int customversion = Envir.LoadCustomVersion;
                    if (version == 9999)//the only real way to tell if the file was made before or after version code got added: assuming nobody had a config option to save more then 10000 sold items :p
                    {
                        version = reader.ReadInt32();
                        customversion = reader.ReadInt32();
                        count = reader.ReadInt32();
                    }
                    else
                        version = Envir.LoadVersion;


                    for (int k = 0; k < count; k++)
                    {
                        UserItem item = new UserItem(reader, version, customversion);
                        if (item.BindItem())
                            UsedGoods.Add(item);
                    }
                }
            }
        }

        //默认的NPC处理
        private void ParseDefault(List<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].ToUpper().StartsWith("[@_")) continue;
                if (Name == "DefaultNPC")
                {
                    //加入活动的点，去到某个点则触发事件
                    //[@_MAPCOORD(3,861,686)]
                    if (lines[i].ToUpper().Contains("MAPCOORD"))
                    {
                        Regex regex = new Regex(@"\((.*?),([0-9]{1,3}),([0-9]{1,3})\)");
                        Match match = regex.Match(lines[i]);

                        if (!match.Success) continue;

                        Map map = Envir.MapList.FirstOrDefault(m => m.Info.FileName == match.Groups[1].Value);

                        if (map == null) continue;

                        Point point = new Point(Convert.ToInt16(match.Groups[2].Value), Convert.ToInt16(match.Groups[3].Value));

                        if (!map.Info.ActiveCoords.Contains(point))
                        {
                            map.Info.ActiveCoords.Add(point);
                        }
                    }
                    //自定义命令
                    if (lines[i].ToUpper().Contains("CUSTOMCOMMAND"))
                    {
                        Regex regex = new Regex(@"\((.*?)\)");
                        Match match = regex.Match(lines[i]);

                        if (!match.Success) continue;

                        SMain.Envir.CustomCommands.Add(match.Groups[1].Value);
                    }
                }
                else if (Name == "MonsterNPC")
                {
                    //配置怪物死亡，重生对应的脚本
                    MonsterInfo MobInfo;
                    if (lines[i].ToUpper().Contains("SPAWN"))
                    {
                        Regex regex = new Regex(@"\((.*?)\)");
                        Match match = regex.Match(lines[i]);

                        if (!match.Success) continue;
                        MobInfo = Envir.GetMonsterInfo(Convert.ToInt16(match.Groups[1].Value));
                        if (MobInfo == null) continue;
                        MobInfo.HasSpawnScript = true;
                    }
                    if (lines[i].ToUpper().Contains("DIE"))
                    {
                        Regex regex = new Regex(@"\((.*?)\)");
                        Match match = regex.Match(lines[i]);

                        if (!match.Success) continue;
                        MobInfo = Envir.GetMonsterInfo(Convert.ToInt16(match.Groups[1].Value));
                        if (MobInfo == null) continue;
                        MobInfo.HasDieScript = true;
                    }
                }

                else if (Name == "RobotNPC")
                {
                    //这个还没用啊
                    //min,hour,day,month
                    if (lines[i].ToUpper().Contains("TIME"))
                    {
                        Regex regex = new Regex(@"\(([0-9]{1,2}),([0-9]{1,2}),([0-9]{1,1}),([0-9]{1,2})\)");
                        Match match = regex.Match(lines[i]);

                        if (!match.Success) continue;
                    }
                }

                NPCPages.AddRange(ParsePages(lines, lines[i]));

            }
        }
        //普通脚本处理
        private void ParseScript(IList<string> lines)
        {
            NPCPages.AddRange(ParsePages(lines));

            ParseGoods(lines);
            ParseTypes(lines);
            ParseQuests(lines);
            ParseCrafting(lines);
        }
        //处理插入
        //#INSERT [SystemScripts\00Default\OnFinishQuests.txt] @Main
        //#这个命令是加到末尾哦
        //如果用这个，则整个文件都是插入
        private List<string> ParseInsert(List<string> lines)
        {
            List<string> newLines = new List<string>();

            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].ToUpper().StartsWith("#INSERT")) continue;

                string[] split = lines[i].Split(' ');

                if (split.Length < 2) continue;

                string path = Path.Combine(Settings.EnvirPath, split[1].Substring(1, split[1].Length - 2));

                if (!File.Exists(path))
                    SMain.Enqueue(string.Format("INSERT File Not Found: {0}, NPC: {1}", path, Info.Name));
                else
                    newLines = File.ReadAllLines(path, EncodingType.GetType(path)).ToList();

                lines.AddRange(newLines);
            }

            lines.RemoveAll(str => str.ToUpper().StartsWith("#INSERT"));

            return lines;
        }

        //替换INCLUDE命令
        //#INCLUDE [SystemScripts/SharedNPCS/Tavern.txt] @Main
        //如果包含的文件不存在，则整个脚本都无效了
        //@Main 的意思是只包含 @Main的块，并且是{}这样包裹的块
        private List<string> ParseInclude(List<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].ToUpper().StartsWith("#INCLUDE")) continue;

                string[] split = lines[i].Split(' ');

                string path = Path.Combine(Settings.EnvirPath, split[1].Substring(1, split[1].Length - 2));
                string page = ("[" + split[2] + "]").ToUpper();

                bool start = false, finish = false;

                var parsedLines = new List<string>();
                //如果包含的文件不存在，则整个脚本都无效了
                if (!File.Exists(path))
                {
                    SMain.Enqueue(string.Format("INCLUDE File Not Found: {0}, NPC: {1}", path, Info.Name));
                    return parsedLines;
                }
                IList<string> extLines = File.ReadAllLines(path, EncodingType.GetType(path));

                for (int j = 0; j < extLines.Count; j++)
                {
                    if (!extLines[j].ToUpper().StartsWith(page)) continue;

                    for (int x = j + 1; x < extLines.Count; x++)
                    {
                        if (extLines[x].Trim() == ("{"))
                        {
                            start = true;
                            continue;
                        }

                        if (extLines[x].Trim() == ("}"))
                        {
                            finish = true;
                            break;
                        }

                        parsedLines.Add(extLines[x]);
                    }
                }

                if (start && finish)
                {
                    lines.InsertRange(i + 1, parsedLines);
                    parsedLines.Clear();
                }
            }

            lines.RemoveAll(str => str.ToUpper().StartsWith("#INCLUDE"));

            return lines;
        }

        //脚本分页处理
        private List<NPCPage> ParsePages(IList<string> lines, string key = MainKey)
        {
            List<NPCPage> pages = new List<NPCPage>();
            List<string> buttons = new List<string>();
            //处理主页
            NPCPage page = ParsePage(lines, key);
            pages.Add(page);

            buttons.AddRange(page.Buttons);

            for (int i = 0; i < buttons.Count; i++)
            {
                string section = buttons[i];

                bool match = pages.Any(t => t.Key.ToUpper() == section.ToUpper());

                if (match) continue;

                page = ParsePage(lines, section);
                buttons.AddRange(page.Buttons);

                pages.Add(page);
            }

            return pages;
        }
        //处理某一页
        private NPCPage ParsePage(IList<string> scriptLines, string sectionName)
        {
            bool nextPage = false, nextSection = false;

            List<string> lines = scriptLines.Where(x => !string.IsNullOrEmpty(x)).ToList();

            NPCPage Page = new NPCPage(sectionName);

            //Cleans arguments out of search page name
            string tempSectionName = Page.ArgumentParse(sectionName);

            //parse all individual pages in a script, defined by sectionName
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                //过滤掉;开始的
                if (line.StartsWith(";")) continue;

                if (!lines[i].ToUpper().StartsWith(tempSectionName.ToUpper())) continue;
                //这里不处理么？
                if(lines[i] == "[@Market]")
                {

                }

                List<string> segmentLines = new List<string>();

                nextPage = false;
                //找到一个页面，现在处理该页面并将其分割成片段
                //Found a page, now process that page and split it into segments
                for (int j = i + 1; j < lines.Count; j++)
                {
                    string nextLine = lines[j];

                    if (j < lines.Count - 1)
                        nextLine = lines[j + 1];
                    else
                        nextLine = "";

                    if (nextLine.StartsWith("[") && nextLine.EndsWith("]"))
                    {
                        nextPage = true;
                    }
                    else if (nextLine.StartsWith("#IF"))
                    {
                        nextSection = true;
                    }

                    if (nextSection || nextPage)
                    {
                        segmentLines.Add(lines[j]);

                        //end of segment, so need to parse it and put into the segment list within the page
                        if (segmentLines.Count > 0)
                        {
                            NPCSegment segment = ParseSegment(Page, segmentLines);

                            List<string> currentButtons = new List<string>();
                            currentButtons.AddRange(segment.Buttons);
                            currentButtons.AddRange(segment.ElseButtons);
                            currentButtons.AddRange(segment.GotoButtons);

                            Page.Buttons.AddRange(currentButtons);
                            Page.SegmentList.Add(segment);
                            segmentLines.Clear();

                            nextSection = false;
                        }

                        if (nextPage) break;

                        continue;
                    }

                    segmentLines.Add(lines[j]);
                }

                //bottom of script reached, add all lines found to new segment
                if (segmentLines.Count > 0)
                {
                    NPCSegment segment = ParseSegment(Page, segmentLines);

                    List<string> currentButtons = new List<string>();
                    currentButtons.AddRange(segment.Buttons);
                    currentButtons.AddRange(segment.ElseButtons);
                    currentButtons.AddRange(segment.GotoButtons);

                    Page.Buttons.AddRange(currentButtons);
                    Page.SegmentList.Add(segment);
                    segmentLines.Clear();
                }
                //这里直接返回了。就是只要够一页，直接就返回了
                return Page;
            }

            return Page;
        }

        //处理片段
        private NPCSegment ParseSegment(NPCPage page, IEnumerable<string> scriptLines)
        {
            List<string>
                checks = new List<string>(),
                acts = new List<string>(),
                say = new List<string>(),
                buttons = new List<string>(),
                elseSay = new List<string>(),
                elseActs = new List<string>(),
                elseButtons = new List<string>(),
                gotoButtons = new List<string>();

            List<string> lines = scriptLines.ToList();
            List<string> currentSay = say, currentButtons = buttons;

            for (int i = 0; i < lines.Count; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) continue;

                if (lines[i].StartsWith(";")) continue;

                if (lines[i].StartsWith("#"))
                {
                    string[] action = lines[i].Remove(0, 1).ToUpper().Trim().Split(' ');
                    switch (action[0])
                    {
                        case "IF":
                            currentSay = checks;
                            currentButtons = null;
                            continue;
                        case "SAY":
                            currentSay = say;
                            currentButtons = buttons;
                            continue;
                        case "ACT":
                            currentSay = acts;
                            currentButtons = gotoButtons;
                            continue;
                        case "ELSESAY":
                            currentSay = elseSay;
                            currentButtons = elseButtons;
                            continue;
                        case "ELSEACT":
                            currentSay = elseActs;
                            currentButtons = gotoButtons;
                            continue;
                        default:
                            throw new NotImplementedException();
                    }
                }

                if (lines[i].StartsWith("[") && lines[i].EndsWith("]")) break;

                if (currentButtons != null)
                {
                    Match match = Regex.Match(lines[i]);
                    while (match.Success)
                    {
                        string argu = match.Groups[1].Captures[0].Value;

                        currentButtons.Add(string.Format("[{0}]", argu));//ToUpper()
                        match = match.NextMatch();
                    }

                    //Check if line has a goto command
                    var parts = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Count() > 1)
                        switch (parts[0].ToUpper())
                        {
                            case "GOTO":
                            case "GROUPGOTO":
                                gotoButtons.Add(string.Format("[{0}]", parts[1].ToUpper()));
                                break;
                            case "TIMERECALL":
                                if (parts.Length > 2)
                                    gotoButtons.Add(string.Format("[{0}]", parts[2].ToUpper()));
                                break;
                            case "TIMERECALLGROUP":
                                if (parts.Length > 2)
                                    gotoButtons.Add(string.Format("[{0}]", parts[2].ToUpper()));
                                break;
                            case "DELAYGOTO":
                                gotoButtons.Add(string.Format("[{0}]", parts[2].ToUpper()));
                                break;
                        }
                }
                
                currentSay.Add(lines[i].TrimEnd());
            }

            NPCSegment segment = new NPCSegment(page, say, buttons, elseSay, elseButtons, gotoButtons);

            for (int i = 0; i < checks.Count; i++)
                segment.ParseCheck(checks[i]);

            for (int i = 0; i < acts.Count; i++)
                segment.ParseAct(segment.ActList, acts[i]);

            for (int i = 0; i < elseActs.Count; i++)
                segment.ParseAct(segment.ElseActList, elseActs[i]);


            currentButtons = new List<string>();
            currentButtons.AddRange(buttons);
            currentButtons.AddRange(elseButtons);
            currentButtons.AddRange(gotoButtons);

            return segment;
        }

        private void ParseTypes(IList<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].ToUpper().StartsWith(TypeKey)) continue;

                while (++i < lines.Count)
                {
                    if (String.IsNullOrEmpty(lines[i])) continue;

                    int index;
                    if (!int.TryParse(lines[i], out index)) return;
                    Types.Add((ItemType)index);
                }
            }
        }
        private void ParseGoods(IList<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].ToUpper().StartsWith(TradeKey)) continue;

                while (++i < lines.Count)
                {
                    if (lines[i].StartsWith("[")) return;
                    if (String.IsNullOrEmpty(lines[i])) continue;

                    var data = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    ItemInfo info = ItemInfo.getItem(data[0]);
                    if (info == null)
                        continue;
                    UserItem goods = new UserItem(info) { CurrentDura = info.Durability, MaxDura = info.Durability };
                    if (goods == null || Goods.Contains(goods))
                    {
                        SMain.Enqueue(string.Format("Could not find Item: {0}, File: {1}", lines[i], Info.FileName));
                        continue;
                    }
                    uint count = 1;
                    if (data.Length == 2)
                        uint.TryParse(data[1], out count);
                    goods.Count = count;
                    goods.UniqueID = (ulong)i;

                    Goods.Add(goods);
                }
            }
        }
        private void ParseQuests(IList<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].ToUpper().StartsWith(QuestKey)) continue;

                while (++i < lines.Count)
                {
                    if (lines[i].StartsWith("[")) return;
                    if (String.IsNullOrEmpty(lines[i])) continue;

                    int index;

                    int.TryParse(lines[i], out index);

                    if (index == 0) continue;

                    QuestInfo info = SMain.Envir.GetQuestInfo(Math.Abs(index));

                    if (info == null) return;

                    if (index > 0)
                        info.NpcIndex = ObjectID;
                    else
                        info.FinishNpcIndex = ObjectID;

                    if (Quests.All(x => x != info))
                        Quests.Add(info);
                }
            }
        }
        private void ParseCrafting(IList<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].ToUpper().StartsWith(RecipeKey)) continue;

                while (++i < lines.Count)
                {
                    if (lines[i].StartsWith("[")) return;
                    if (String.IsNullOrEmpty(lines[i])) continue;

                    var data = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    ItemInfo info = ItemInfo.getItem(data[0]);
                    if (info == null)
                        continue;

                    RecipeInfo recipe = Envir.RecipeInfoList.SingleOrDefault(x => x.MatchItem(info.Index));

                    if (recipe == null)
                    {
                        SMain.Enqueue(string.Format("Could not find recipe: {0}, File: {1}", lines[i], Info.FileName));
                        continue;
                    }

                    if (recipe.Ingredients.Count == 0)
                    {
                        SMain.Enqueue(string.Format("Could not find ingredients: {0}, File: {1}", lines[i], Info.FileName));
                        continue;
                    }

                    CraftGoods.Add(recipe);
                }
            }
        }

        public void Call(MonsterObject Monster, string key)//run a semi limited npc script (wont let you do stuff like checkgroup/guild etc)
        {
            key = key.ToUpper();

            for (int i = 0; i < NPCPages.Count; i++)
            {
                NPCPage page = NPCPages[i];
                if (!String.Equals(page.Key, key, StringComparison.CurrentCultureIgnoreCase)) continue;

                foreach (NPCSegment segment in page.SegmentList)
                {
                    if (page.BreakFromSegments)
                    {
                        page.BreakFromSegments = false;
                        break;
                    }

                    ProcessSegment(Monster, page, segment);
                }
            }
        }

        public void Call(string key) //run a verry limited npc script (should really only be used to spawn mobs or something)
        {
            key = key.ToUpper();

            for (int i = 0; i < NPCPages.Count; i++)
            {
                NPCPage page = NPCPages[i];
                if (!String.Equals(page.Key, key, StringComparison.CurrentCultureIgnoreCase)) continue;

                foreach (NPCSegment segment in page.SegmentList)
                {
                    if (page.BreakFromSegments)
                    {
                        page.BreakFromSegments = false;
                        break;
                    }

                    ProcessSegment(page, segment);
                }
            }
        }

        //玩家触发NPC命令
        public void Call(PlayerObject player, string key)
        {
            key = key.ToUpper();

            if (!player.NPCDelayed)
            {
                if (key != MainKey) // && ObjectID != player.DefaultNPC.ObjectID
                {
                    if (player.NPCID != ObjectID) return;

                    bool found = false;

                    foreach (NPCSegment segment in player.NPCPage.SegmentList)
                    {
                        bool result;
                        if (!player.NPCSuccess.TryGetValue(segment, out result)) break; //no result for segement ?

                        if ((result ? segment.Buttons : segment.ElseButtons).Any(s => s.ToUpper() == key)) //key is already uppercase
                            found = true;
                    }

                    if (!found)
                    {
                        SMain.Enqueue(string.Format("Player: {0} was prevented access to NPC key: '{1}' ", player.Name, key));
                        return;
                    }
                }
            }
            else
            {
                player.NPCDelayed = false;
            }

            if (key.StartsWith("[@@") && player.NPCInputStr == string.Empty)
            {
                //send off packet to request input
                player.Enqueue(new S.NPCRequestInput { NPCID = ObjectID, PageName = key });
                return;
            }

            for (int i = 0; i < NPCPages.Count; i++)
            {
                NPCPage page = NPCPages[i];
                if (!String.Equals(page.Key, key, StringComparison.CurrentCultureIgnoreCase)) continue;

                player.NPCSpeech = new List<string>();
                player.NPCSuccess.Clear();

                foreach (NPCSegment segment in page.SegmentList)
                {
                    if (page.BreakFromSegments)
                    {
                        page.BreakFromSegments = false;
                        break;
                    }

                    ProcessSegment(player, page, segment);
                }

                Response(player, page);
            }


            player.NPCInputStr = string.Empty;
        }

        private void Response(PlayerObject player, NPCPage page)
        {
            player.Enqueue(new S.NPCResponse { Page = player.NPCSpeech });

            ProcessSpecial(player, page);
        }

        private void ProcessSegment(PlayerObject player, NPCPage page, NPCSegment segment)
        {
            player.NPCID = ObjectID;
            player.NPCSuccess.Add(segment, segment.Check(player));
            player.NPCPage = page;
        }

        private void ProcessSegment(MonsterObject Monster, NPCPage page, NPCSegment segment)
        {
            segment.Check(Monster);
        }

        private void ProcessSegment(NPCPage page, NPCSegment segment)
        {
            segment.Check();
        }

        //特殊处理
        private void ProcessSpecial(PlayerObject player, NPCPage page)
        {
            List<UserItem> allGoods = new List<UserItem>();

            switch (page.Key.ToUpper())
            {
                case BuyKey:
                    for (int i = 0; i < Goods.Count; i++)
                        player.CheckItem(Goods[i]);

                    player.Enqueue(new S.NPCGoods { List = Goods, Rate = PriceRate(player), Type = PanelType.Buy });
                    break;
                case SellKey:
                    player.Enqueue(new S.NPCSell());
                    break;
                case BuySellKey:
                    for (int i = 0; i < Goods.Count; i++)
                        player.CheckItem(Goods[i]);

                    player.Enqueue(new S.NPCGoods { List = Goods, Rate = PriceRate(player), Type = PanelType.Buy });
                    player.Enqueue(new S.NPCSell());
                    break;
                case RepairKey:
                    player.Enqueue(new S.NPCRepair { Rate = PriceRate(player) });
                    break;
                case SRepairKey:
                    player.Enqueue(new S.NPCSRepair { Rate = PriceRate(player) });
                    break;
                case CraftKey:
                    for (int i = 0; i < CraftGoods.Count; i++)
                        player.CheckItemInfo(CraftGoods[i].Item.Info);

                    player.Enqueue(new S.NPCGoods { List = (from x in CraftGoods where x.CanCraft(player) select x.Item).ToList(), Rate = PriceRate(player), Type = PanelType.Craft });
                    break;
                case RefineKey:
                    if (player.Info.CurrentRefine != null)
                    {
                        player.ReceiveChat("You're already refining an item.", ChatType.System);
                        player.Enqueue(new S.NPCRefine { Rate = (Settings.RefineCost), Refining = true });
                        break;
                    }
                    else
                        player.Enqueue(new S.NPCRefine { Rate = (Settings.RefineCost), Refining = false });
                    break;
                case RefineCheckKey:
                    player.Enqueue(new S.NPCCheckRefine());
                    break;
                case RefineCollectKey:
                    player.CollectRefine();
                    break;
                case ReplaceWedRingKey:
                    player.Enqueue(new S.NPCReplaceWedRing { Rate = Settings.ReplaceWedRingCost });
                    break;
                case StorageKey:
                    player.SendStorage();
                    player.Enqueue(new S.NPCStorage());
                    break;
                case BuyBackKey:
                    if (!BuyBack.ContainsKey(player.Name)) BuyBack[player.Name] = new List<UserItem>();

                    for (int i = 0; i < BuyBack[player.Name].Count; i++)
                    {
                        player.CheckItem(BuyBack[player.Name][i]);
                    }

                    player.Enqueue(new S.NPCGoods { List = BuyBack[player.Name], Rate = PriceRate(player), Type = PanelType.Buy });
                    break;
                case BuyUsedKey:
                    for (int i = 0; i < UsedGoods.Count; i++)
                        player.CheckItem(UsedGoods[i]);

                    player.Enqueue(new S.NPCGoods { List = UsedGoods, Rate = PriceRate(player), Type = PanelType.Buy });
                    break;
                case ConsignKey:
                    player.Enqueue(new S.NPCConsign());
                    break;
                case MarketKey:
                    player.UserMatch = false;
                    player.MarketSearch(string.Empty);
                    break;
                case ConsignmentsKey:
                    player.UserMatch = true;
                    player.MarketSearch(string.Empty);
                    break;
                case GuildCreateKey:
                    if (player.Info.Level < Settings.Guild_RequiredLevel)
                    {
                        player.ReceiveChat(String.Format("You have to be at least level {0} to create a guild.", Settings.Guild_RequiredLevel), ChatType.System);
                    }
                    if (player.MyGuild == null)
                    {
                        player.CanCreateGuild = true;
                        player.Enqueue(new S.GuildNameRequest());
                    }
                    else
                        player.ReceiveChat("You are already part of a guild.", ChatType.System);
                    break;
                case RequestWarKey:
                    if (player.MyGuild != null)
                    {
                        if (player.MyGuildRank != player.MyGuild.Ranks[0])
                        {
                            player.ReceiveChat("You must be the leader to request a war.", ChatType.System);
                            return;
                        }
                        player.Enqueue(new S.GuildRequestWar());
                    }
                    else
                    {
                        player.ReceiveChat("You are not in a guild.", ChatType.System);
                    }
                    break;
                case SendParcelKey:
                    player.Enqueue(new S.MailSendRequest());
                    break;
                case CollectParcelKey:

                    sbyte result = 0;

                    if (player.GetMailAwaitingCollectionAmount() < 1)
                    {
                        result = -1;
                    }
                    else
                    {
                        foreach (var mail in player.Info.Mail)
                        {
                            if (mail.Parcel) mail.Collected = true;
                        }
                    }
                    player.Enqueue(new S.ParcelCollected { Result = result });
                    player.GetMail();
                    break;
                case AwakeningKey:
                    player.Enqueue(new S.NPCAwakening());
                    break;
                case DisassembleKey:
                    player.Enqueue(new S.NPCDisassemble());
                    break;
                case DowngradeKey:
                    player.Enqueue(new S.NPCDowngrade());
                    break;
                case ResetKey:
                    player.Enqueue(new S.NPCReset());
                    break;
                case PearlBuyKey:
                    for (int i = 0; i < Goods.Count; i++)
                        player.CheckItem(Goods[i]);

                    player.Enqueue(new S.NPCPearlGoods { List = Goods, Rate = PriceRate(player) });
                    break;
            }
        }

        #region overrides
        public override void Process(DelayedAction action)
        {
            throw new NotSupportedException();
        }

        public override bool IsAttackTarget(PlayerObject attacker)
        {
            // throw new NotSupportedException();
            return false;
        }
        public override bool IsFriendlyTarget(PlayerObject ally)
        {
            throw new NotSupportedException();
        }
        public override bool IsFriendlyTarget(MonsterObject ally)
        {
            throw new NotSupportedException();
        }
        public override bool IsAttackTarget(MonsterObject attacker)
        {
            //   throw new NotSupportedException();
            return false;
        }

        public override int Attacked(PlayerObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true)
        {
            throw new NotSupportedException();
        }

        public override int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility)
        {
            throw new NotSupportedException();
        }

        public override int Struck(int damage, DefenceType type = DefenceType.ACAgility)
        {
            throw new NotSupportedException();
        }

        public override void SendHealth(PlayerObject player)
        {
            throw new NotSupportedException();
        }

        public override void Die()
        {
            throw new NotSupportedException();
        }

        public override int Pushed(MapObject pusher, MirDirection dir, int distance)
        {
            throw new NotSupportedException();
        }

        public override ushort Level
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override void ReceiveChat(string text, ChatType type)
        {
            throw new NotSupportedException();
        }

        public void Turn(MirDirection dir)
        {
            Direction = dir;

            Broadcast(new S.ObjectTurn { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
        }

        public override void Process()
        {
            base.Process();
            //NPC每10秒转一次方向
            if (Envir.Time > TurnTime)
            {
                TurnTime = Envir.Time + TurnDelay;
                Turn((MirDirection)RandomUtils.Next(3));
            }
            //一个小时一次，这个是食材？食材处理？
            if (Envir.Time > UsedGoodsTime)
            {
                UsedGoodsTime = SMain.Envir.Time + (Settings.Minute * Settings.GoodsBuyBackTime);
                ProcessGoods();
            }

            //判断NPC是否可看见
            if (Envir.Time > VisTime)
            {
                VisTime = Envir.Time + (Settings.Minute);

                if (Info.DayofWeek != "" && Info.DayofWeek != DateTime.Now.DayOfWeek.ToString())
                {
                    if (Visible) Hide();
                }
                else
                {
                    int StartTime = ((Info.HourStart * 60) + Info.MinuteStart);
                    int FinishTime = ((Info.HourEnd * 60) + Info.MinuteEnd);
                    int CurrentTime = ((DateTime.Now.Hour * 60) + DateTime.Now.Minute);

                    if (Info.TimeVisible)
                    {
                        if (StartTime > CurrentTime || FinishTime <= CurrentTime)
                        {
                            if (Visible) Hide();
                        }
                        else if (StartTime <= CurrentTime && FinishTime > CurrentTime)
                        {
                            if (!Visible) Show();
                        }
                    }
                }
            }
        }

        public void ProcessGoods(bool clear = false)
        {
            if (!Settings.GoodsOn) return;

            List<UserItem> deleteList = new List<UserItem>();

            foreach (var playerGoods in BuyBack)
            {
                List<UserItem> items = playerGoods.Value;

                for (int i = 0; i < items.Count; i++)
                {
                    UserItem item = items[i];

                    if (DateTime.Compare(item.BuybackExpiryDate.AddMinutes(Settings.GoodsBuyBackTime), Envir.Now) <= 0 || clear)
                    {
                        deleteList.Add(BuyBack[playerGoods.Key][i]);

                        if (UsedGoods.Count >= Settings.GoodsMaxStored)
                        {
                            UserItem nonAddedItem = UsedGoods.FirstOrDefault(e => e.IsAdded == false);

                            if (nonAddedItem != null)
                            {
                                UsedGoods.Remove(nonAddedItem);
                            }
                            else
                            {
                                UsedGoods.RemoveAt(0);
                            }
                        }

                        UsedGoods.Add(item);
                        NeedSave = true;
                    }
                }

                for (int i = 0; i < deleteList.Count; i++)
                {
                    BuyBack[playerGoods.Key].Remove(deleteList[i]);
                }
            }
        }

        public override void SetOperateTime()
        {
            long time = Envir.Time + 2000;

            if (TurnTime < time && TurnTime > Envir.Time)
                time = TurnTime;

            if (OwnerTime < time && OwnerTime > Envir.Time)
                time = OwnerTime;

            if (ExpireTime < time && ExpireTime > Envir.Time)
                time = ExpireTime;

            if (PKPointTime < time && PKPointTime > Envir.Time)
                time = PKPointTime;

            if (LastHitTime < time && LastHitTime > Envir.Time)
                time = LastHitTime;

            if (EXPOwnerTime < time && EXPOwnerTime > Envir.Time)
                time = EXPOwnerTime;

            if (BrownTime < time && BrownTime > Envir.Time)
                time = BrownTime;

            for (int i = 0; i < ActionList.Count; i++)
            {
                if (ActionList[i].Time >= time && ActionList[i].Time > Envir.Time) continue;
                time = ActionList[i].Time;
            }

            for (int i = 0; i < PoisonList.Count; i++)
            {
                if (PoisonList[i].TickTime >= time && PoisonList[i].TickTime > Envir.Time) continue;
                time = PoisonList[i].TickTime;
            }

            for (int i = 0; i < Buffs.Count; i++)
            {
                if (Buffs[i].ExpireTime >= time && Buffs[i].ExpireTime > Envir.Time) continue;
                time = Buffs[i].ExpireTime;
            }


            if (OperateTime <= Envir.Time || time < OperateTime)
                OperateTime = time;
        }

        public void Hide()
        {
            CurrentMap.Broadcast(new S.ObjectRemove { ObjectID = ObjectID }, CurrentLocation);
            Visible = false;
        }

        public void Show()
        {
            Visible = true;
            for (int i = CurrentMap.Players.Count - 1; i >= 0; i--)
            {
                PlayerObject player = CurrentMap.Players[i];

                if (Functions.InRange(CurrentLocation, player.CurrentLocation, Globals.DataRange))
                {
                    CheckVisible(player, true);
                    if(player.CheckStacked())
                    {
                        player.StackingTime = Envir.Time + 1000;
                        player.Stacking = true;
                    }
                }
            }
        }

        public override Packet GetInfo()
        {
            return new S.ObjectNPC
            {
                ObjectID = ObjectID,
                Name = Name,
                NameColour = NameColour,
                Image = Info.Image,
                Colour = Info.Colour,
                Location = CurrentLocation,
                Direction = Direction,
                QuestIDs = (from q in Quests
                            select q.Index).ToList()
            };
        }

        public Packet GetUpdateInfo()
        {
            return new S.NPCImageUpdate
            {
                ObjectID = ObjectID,
                Image = Info.Image,
                Colour = Info.Colour
            };
        }

        public override void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false, bool ignoreDefence = true)
        {
            throw new NotSupportedException();
        }

        public override string Name
        {
            get { return Info.Name; }
            set { throw new NotSupportedException(); }
        }

        public override bool Blocking
        {
            get { return Visible; }
        }


        public void CheckVisible(PlayerObject Player, bool Force = false)
        {
            bool CanSee;

            VisibleLog.TryGetValue(Player.Info.Index, out CanSee);

            if (Conq != null && Conq.WarIsOn)
            {
                if (CanSee) CurrentMap.Broadcast(new S.ObjectRemove { ObjectID = ObjectID }, CurrentLocation, Player);
                VisibleLog[Player.Info.Index] = false;
                return;
            }

            if (Info.FlagNeeded != 0 && !Player.Info.Flags[Info.FlagNeeded])
            {
                if (CanSee) CurrentMap.Broadcast(new S.ObjectRemove { ObjectID = ObjectID }, CurrentLocation, Player);
                VisibleLog[Player.Info.Index] = false;
                return;
            }

            if (Info.MinLev != 0 && Player.Level < Info.MinLev || Info.MaxLev != 0 && Player.Level > Info.MaxLev)
            {
                if (CanSee) CurrentMap.Broadcast(new S.ObjectRemove { ObjectID = ObjectID }, CurrentLocation, Player);
                VisibleLog[Player.Info.Index] = false;
                return;
            }

            if (Info.ClassRequired != "" && Player.Class.ToString() != Info.ClassRequired)
            {
                if (CanSee) CurrentMap.Broadcast(new S.ObjectRemove { ObjectID = ObjectID }, CurrentLocation, Player);
                VisibleLog[Player.Info.Index] = false;
                return;
            }

            if (Visible && !CanSee) CurrentMap.Broadcast(GetInfo(), CurrentLocation, Player);
            else if (Force && Visible) CurrentMap.Broadcast(GetInfo(), CurrentLocation, Player);

            VisibleLog[Player.Info.Index] = true;

        }

        public override int CurrentMapIndex { get; set; }

        public override Point CurrentLocation
        {
            get { return Info.Location; }
            set { throw new NotSupportedException(); }
        }

        public override MirDirection Direction { get; set; }

        public override uint Health
        {
            get { throw new NotSupportedException(); }
        }

        public override uint MaxHealth
        {
            get { throw new NotSupportedException(); }
        }
        #endregion

        public void Buy(PlayerObject player, ulong index, uint count)
        {
            UserItem goods = null;

            for (int i = 0; i < Goods.Count; i++)
            {
                if (Goods[i].UniqueID != index) continue;
                goods = Goods[i];
                break;
            }

            bool isUsed = false;
            if (goods == null)
            {
                for (int i = 0; i < UsedGoods.Count; i++)
                {
                    if (UsedGoods[i].UniqueID != index) continue;
                    goods = UsedGoods[i];
                    isUsed = true;
                    break;
                }
            }

            bool isBuyBack = false;
            if (goods == null)
            {
                if (!BuyBack.ContainsKey(player.Name)) BuyBack[player.Name] = new List<UserItem>();
                for (int i = 0; i < BuyBack[player.Name].Count; i++)
                {
                    if (BuyBack[player.Name][i].UniqueID != index) continue;
                    goods = BuyBack[player.Name][i];
                    isBuyBack = true;
                    break;
                }
            }

            if (goods == null || count == 0 || count > goods.Info.StackSize) return;

            goods.Count = count;

            uint cost = goods.Price();
            cost = (uint)(cost * PriceRate(player));
            uint baseCost = (uint)(goods.Price() * PriceRate(player, true));

            if (player.NPCPage.Key.ToUpper() == PearlBuyKey)//pearl currency
            {
                if (cost > player.Info.PearlCount) return;
            }
            else if (cost > player.Account.Gold) return;

            UserItem item = (isBuyBack || isUsed) ? goods : goods.Info.CreateFreshItem();
            item.Count = goods.Count;

            if (!player.CanGainItem(item)) return;

            if (player.NPCPage.Key.ToUpper() == PearlBuyKey)//pearl currency
            {
                player.Info.PearlCount -= (int)cost;
            }
            else
            {
                player.Account.Gold -= cost;
                player.Enqueue(new S.LoseGold { Gold = cost });
                if (Conq != null) Conq.GoldStorage += (cost - baseCost);
            }
            player.GainItem(item);

            if (isUsed)
            {
                UsedGoods.Remove(goods); //If used or buyback will destroy whole stack instead of reducing to remaining quantity

                List<UserItem> newGoodsList = new List<UserItem>();
                newGoodsList.AddRange(Goods);
                newGoodsList.AddRange(UsedGoods);

                NeedSave = true;

                player.Enqueue(new S.NPCGoods { List = newGoodsList, Rate = PriceRate(player) });
            }

            if (isBuyBack)
            {
                BuyBack[player.Name].Remove(goods); //If used or buyback will destroy whole stack instead of reducing to remaining quantity
                player.Enqueue(new S.NPCGoods { List = BuyBack[player.Name], Rate = PriceRate(player) });
            }
        }
        public void Sell(PlayerObject player, UserItem item)
        {
            /* Handle Item Sale */
            if (!BuyBack.ContainsKey(player.Name)) BuyBack[player.Name] = new List<UserItem>();

            if (BuyBack[player.Name].Count >= Settings.GoodsBuyBackMaxStored)
                BuyBack[player.Name].RemoveAt(0);

            item.BuybackExpiryDate = Envir.Now;
            BuyBack[player.Name].Add(item);
        }


        public void Craft(PlayerObject player, ulong index, uint count, int[] slots)
        {
            S.CraftItem p = new S.CraftItem();

            RecipeInfo recipe = null;

            for (int i = 0; i < CraftGoods.Count; i++)
            {
                if (CraftGoods[i].Item.UniqueID != index) continue;
                recipe = CraftGoods[i];
                break;
            }

            UserItem goods = recipe.Item;

            if (goods == null || count == 0 || count > goods.Info.StackSize)
            {
                player.Enqueue(p);
                return;
            }

            bool hasItems = true;

            List<int> usedSlots = new List<int>();

            //Check Items
            foreach (var ingredient in recipe.Ingredients)
            {
                uint amount = ingredient.Count * count;

                for (int i = 0; i < slots.Length; i++)
                {
                    int slot = slots[i];

                    if (usedSlots.Contains(slot)) continue;

                    if (slot < 0 || slot > player.Info.Inventory.Length) continue;

                    UserItem item = player.Info.Inventory[slot];

                    if (item == null || item.Info != ingredient.Info) continue;

                    usedSlots.Add(slot);

                    if (amount <= item.Count)
                    {
                        amount = 0;
                    }
                    else
                    {
                        hasItems = false;
                    }

                    break;
                }

                if (amount > 0)
                {
                    hasItems = false;
                    break;
                }
            }

            if (!hasItems)
            {
                player.Enqueue(p);
                return;
            }

            UserItem craftedItem = goods.Info.CreateFreshItem();
            craftedItem.Count = count;

            if (!player.CanGainItem(craftedItem))
            {
                player.Enqueue(p);
                return;
            }
            
            //Take Items
            foreach (var ingredient in recipe.Ingredients)
            {
                uint amount = ingredient.Count * count;

                for (int i = 0; i < slots.Length; i++)
                {
                    int slot = slots[i];

                    if (slot < 0) continue;

                    UserItem item = player.Info.Inventory[slot];

                    if (item == null || item.Info != ingredient.Info) continue;

                    if (item.Count > amount)
                    {
                        player.Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = amount });
                        player.Info.Inventory[slot].Count -= amount;
                        break;
                    }
                    else
                    {
                        player.Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                        amount -= item.Count;
                        player.Info.Inventory[slot] = null;
                    }

                    break;
                }
            }

            //Give Item
            player.GainItem(craftedItem);

            p.Success = true;
            player.Enqueue(p);
        }
    }



    public class NPCChecks
    {
        public CheckType Type;
        public List<string> Params = new List<string>();

        public NPCChecks(CheckType check, params string[] p)
        {
            Type = check;

            for (int i = 0; i < p.Length; i++)
                Params.Add(p[i]);
        }
    }
    /// <summary>
    /// npc的命令执行
    /// </summary>
    public class NPCActions
    {
        public ActionType Type;
        public List<string> Params = new List<string>();

        public NPCActions(ActionType action, params string[] p)
        {
            Type = action;

            Params.AddRange(p);
        }
    }

    public enum ActionType
    {
        Move,
        InstanceMove,
        GiveGold,
        TakeGold,
        GiveGuildGold,
        TakeGuildGold,
        GiveCredit,
        TakeCredit,
        GiveItem,
        TakeItem,
        GiveExp,
        GivePet,
        ClearPets,
        AddNameList,
        DelNameList,
        ClearNameList,
        GiveHP,
        GiveMP,
        ChangeLevel,
        SetPkPoint,
        ReducePkPoint,
        IncreasePkPoint,
        ChangeGender,
        ChangeClass,
        LocalMessage,
        Goto,
        GiveSkill,
        RemoveSkill,
        Set,
        Param1,
        Param2,
        Param3,
        Mongen,
        TimeRecall,
        TimeRecallGroup,
        BreakTimeRecall,
        MonClear,
        GroupRecall,
        GroupTeleport,
        DelayGoto,
        Mov,
        Calc,
        GiveBuff,
        RemoveBuff,
        AddToGuild,
        RemoveFromGuild,
        RefreshEffects,
        ChangeHair,
        CanGainExp,
        ComposeMail,
        AddMailItem,
        AddMailGold,
        SendMail,
        GroupGoto,
        EnterMap,
        GivePearls,
        TakePearls,
        MakeWeddingRing,
        ForceDivorce,
        GlobalMessage,
        LoadValue,
        SaveValue,
        RemovePet,
        ConquestGuard,
        ConquestGate,
        ConquestWall,
        ConquestSiege,
        TakeConquestGold,
        SetConquestRate,
        StartConquest,
        ScheduleConquest,
        OpenGate,
        CloseGate,
        Break,
        AddGuildNameList,
        DelGuildNameList,
        ClearGuildNameList,
    }
    public enum CheckType
    {
        IsAdmin,
        Level,
        CheckItem,
        CheckGold,
        CheckGuildGold,
        CheckCredit,
        CheckGender,
        CheckClass,
        CheckDay,
        CheckHour,
        CheckMinute,
        CheckNameList,
        CheckPkPoint,
        CheckRange,
        Check,
        CheckHum,
        CheckMon,
        CheckExactMon,
        Random,
        Groupleader,
        GroupCount,
        GroupCheckNearby,
        PetLevel,
        PetCount,
        CheckCalc,
        InGuild,
        CheckMap,
        CheckQuest,
        CheckRelationship,
        CheckWeddingRing,
        CheckPet,
        HasBagSpace,
		IsNewHuman,
        CheckConquest,
        AffordGuard,
        AffordGate,
        AffordWall,
        AffordSiege,
        CheckPermission,
        ConquestAvailable,
        ConquestOwner,
        CheckGuildNameList
    }
}
