﻿﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Client.MirGraphics;
using Client.MirScenes;
using Client.MirSounds;
using S = ServerPackets;
using Client.MirControls;

namespace Client.MirObjects
{
    /// <summary>
    /// 怪物对象
    /// 这个包括各种怪物对应的图片，动作等都在这里写死了。
    /// 具体怪物对应的图片，动作这些，应该在服务器端定义的？
    /// 
    /// </summary>
    class MonsterObject : MapObject
    {
        public override ObjectType Race
        {
            get { return ObjectType.Monster; }
        }
        //这里的AI64 AI81分别是什么？
        //81朝向左边就不阻挡？

        private bool _Blocking;

        public override bool Blocking
        {
            get {
                if (GameScene.Scene.MapControl.InSafeZone(CurrentLocation))
                {
                    return false;
                }
                return AI == 64 || (AI == 81 && Direction == (MirDirection)6) ? false : !Dead;
            }
        }

        public Point ManualLocationOffset
        {
            get
            {
                switch (BaseImage)
                {
                    case Monster.EvilMir:
                        return new Point(-21, -15);
                    case Monster.PalaceWall2:
                    case Monster.PalaceWallLeft:
                    case Monster.PalaceWall1:
                    case Monster.GiGateSouth:
                    case Monster.GiGateWest:
                    case Monster.SSabukWall1:
                    case Monster.SSabukWall2:
                    case Monster.SSabukWall3:
                        return new Point(-10, 0);
                        break;
                    case Monster.GiGateEast:
                        return new Point(-45, 7);
                        break;
                    default:
                        return new Point(0, 0);
                }
            }
        }

        public Monster BaseImage;
        public byte Effect;
        public bool Skeleton;

        //帧动画
        public FrameSet Frames;
        public Frame Frame;
        public int FrameIndex, FrameInterval, EffectFrameIndex, EffectFrameInterval;

        public uint TargetID;
        public Point TargetPoint;

        public bool Stoned;
        public byte Stage;//扩展字段放在这里哈
        public int BaseSound;

        public long ShockTime;
        public bool BindingShotCenter;

        public Color OldNameColor;

        public MonsterObject(uint objectID)
            : base(objectID)
        {
        }
        //这个是否服务器端返回的怪物信息
        public void Load(S.ObjectMonster info, bool update = false)
        {
            Name = info.Name;
            NameColour = info.NameColour;
            BaseImage = info.Image;

            OldNameColor = NameColour;

            CurrentLocation = info.Location;
            MapLocation = info.Location;
            if (!update) GameScene.Scene.MapControl.AddObject(this);



            Effect = info.Effect;
            AI = info.AI;
            Light = info.Light;

            Direction = info.Direction;
            Dead = info.Dead;
            Poison = info.Poison;
            Skeleton = info.Skeleton;
            Hidden = info.Hidden;
            ShockTime = CMain.Time + info.ShockTime;
            BindingShotCenter = info.BindingShotCenter;



            if (Stage != info.ExtraByte)
            {
                switch (BaseImage)
                {
                    case Monster.GreatFoxSpirit:
                        if (update)
                        {
                            Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.GreatFoxSpirit], 335, 20, 3000, this));
                            SoundManager.PlaySound(BaseSound + 8);
                        }
                        break;
                    case Monster.GeneralJinmYo://猫妖将军，狂暴
                        Effects.Clear();
                        if (Stage == 1)
                        {
                            Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.GeneralJinmYo], 529, 7, 1400, this) { Repeat = true });
                        }
                        break;
                    case Monster.Monster413:
                        Effects.Clear();
                        if (info.ExtraByte == 1)
                        {
                            Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster413], 496, 8, 1600, this) { Repeat = true });
                        }
                        break;
                    case Monster.Monster421:
                        Effects.Clear();
                        if (info.ExtraByte == 1)
                        {
                            Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster421], 637, 8, 1600, this) { Repeat = true });
                        }
                        break;
                    case Monster.Monster429:
                        Effects.Clear();
                        if (info.ExtraByte == 1)
                        {
                            Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster429], 542, 8, 1600, this) { Repeat = true });
                        }
                        break;
                    case Monster.Monster439:
                        Effects.Clear();
                        if (info.ExtraByte == 1)
                        {
                            Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster439], 550, 6, 1200, this) { Repeat = true });
                        }
                        break;

                    case Monster.Monster454:
                        Effects.Clear();
                        if (info.ExtraByte == 1)
                        {
                            Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster454], 1054, 7, 1400, this) { Repeat = true });
                        }
                        break;
                    case Monster.HellLord://太子
                        {
                            Effects.Clear();

                            var effects = MapControl.Effects.Where(x => x.Library == Libraries.Monsters[(ushort)Monster.HellLord]);

                            foreach (var effect in effects)
                                effect.Repeat = false;

                            if (info.ExtraByte > 3)
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.HellLord], 21, 6, 600, this) { Repeat = true });
                            else
                            {
                                if (info.ExtraByte > 2)
                                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.HellLord], 105, 6, 600, new Point(100, 84)) { Repeat = true });
                                if (info.ExtraByte > 1)
                                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.HellLord], 111, 6, 600, new Point(96, 81)) { Repeat = true });
                                if (info.ExtraByte > 0)
                                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.HellLord], 123, 6, 600, new Point(93, 84)) { Repeat = true });
                            }
                        }
                        break;
                }
            }

            Stage = info.ExtraByte;

            switch (BaseImage)
            {
                case Monster.EvilMir:
                case Monster.DragonStatue:
                    BodyLibrary = Libraries.Dragon;
                    break;
                case Monster.EvilMirBody:
                    break;
                case Monster.BabyPig:
                case Monster.Chick:
                case Monster.Kitten:
                case Monster.BabySkeleton:
                case Monster.Baekdon:
                case Monster.Wimaen:
                case Monster.BlackKitten:
                case Monster.BabyDragon:
                case Monster.OlympicFlame:
                case Monster.BabySnowMan:
                case Monster.Frog:
                case Monster.BabyMonkey:
                case Monster.AngryBird:
                case Monster.Foxey:
                    BodyLibrary = Libraries.Pets[((ushort)BaseImage) - 10000];
                    break;
                case Monster.SabukGate:
                case Monster.PalaceWallLeft:
                case Monster.PalaceWall1:
                case Monster.PalaceWall2:
                    BodyLibrary = Libraries.Effect;
                    break;
                case Monster.SSabukWall1:
                case Monster.SSabukWall2:
                case Monster.SSabukWall3:
                    BodyLibrary = Libraries.Gates[0];
                    break;
                case Monster.GiGateSouth:
                case Monster.GiGateEast:
                case Monster.GiGateWest:
                    BodyLibrary = Libraries.Gates[1];
                    break;
                case Monster.HellBomb1:
                case Monster.HellBomb2:
                case Monster.HellBomb3:
                    BodyLibrary = Libraries.Monsters[(ushort)Monster.HellLord];
                    break;
                default:
                    BodyLibrary = Libraries.Monsters[(ushort)BaseImage];
                    break;
            }
            
            if (Skeleton)
                ActionFeed.Add(new QueuedAction { Action = MirAction.Skeleton, Direction = Direction, Location = CurrentLocation });
            else if (Dead)
                ActionFeed.Add(new QueuedAction { Action = MirAction.Dead, Direction = Direction, Location = CurrentLocation });

            BaseSound = (ushort)BaseImage * 10;

            initFrame(info);

            SetAction();
            SetCurrentEffects();

            if (CurrentAction == MirAction.Standing)
            {
                PlayAppearSound();
                FrameIndex = CMain.Random.Next(Frame.Count);
            }
            else if(CurrentAction == MirAction.SitDown)
            {
                PlayAppearSound();
            }

            NextMotion -= NextMotion % 100;

            if (Settings.Effect)
            {
                switch (BaseImage)
                {
                    case Monster.Weaver:
                    case Monster.VenomWeaver:
                    case Monster.ArmingWeaver:
                    case Monster.ValeBat:
                    case Monster.CrackingWeaver:
                    case Monster.GreaterWeaver:
                    case Monster.CrystalWeaver:
                        Effects.Add(new Effect(Libraries.Effect, 680, 20, 20 * Frame.Interval, this) { DrawBehind = true, Repeat = true });
                        break;
                }
            }
        }

        //这个是服务器返回的怪物更新信息，比如怪物的状态更新，狂暴等更新
        public void Change(S.ObjectMonsterChange info)
        {
            Name = info.Name;
            NameColour = info.NameColour;
            BaseImage = info.Image;

            OldNameColor = NameColour;

            Effect = info.Effect;
            AI = info.AI;
            Light = info.Light;

            Dead = info.Dead;
            Poison = info.Poison;
            Skeleton = info.Skeleton;
            Hidden = info.Hidden;
            ShockTime = CMain.Time + info.ShockTime;
            BindingShotCenter = info.BindingShotCenter;

            Stage = info.ExtraByte;
        }

        //固化所有怪物的帧数
        private void initFrame(S.ObjectMonster info)
        {
            //针对部分怪切换形态
            if(BaseImage== Monster.Monster446 && info.ExtraByte==1)
            {
                if(info.ExtraByte == 1)
                {
                    Frames = FrameSet.MonstersMap[Monster.Monster20446];
                }
                else
                {
                    Frames = FrameSet.MonstersMap[Monster.Monster446];
                }
                return;
            }

            if (FrameSet.MonstersMap.ContainsKey(BaseImage))
            {
                Frames = FrameSet.MonstersMap[BaseImage];
                return;
            }
            switch (BaseImage)
            {
                case Monster.Guard:
                case Monster.Guard2:
                    Frames = FrameSet.Monsters[0];
                    break;
                case Monster.Hen:
                case Monster.Deer:
                case Monster.Sheep:
                case Monster.Wolf:
                case Monster.Pig:
                case Monster.Bull:
                case Monster.DarkBrownWolf:
                    Frames = FrameSet.Monsters[1];
                    break;
                case Monster.Scarecrow:
                case Monster.HookingCat:
                case Monster.RakingCat:
                case Monster.Yob:
                case Monster.Oma:
                case Monster.SpittingSpider:
                case Monster.OmaFighter:
                case Monster.OmaWarrior:
                case Monster.CaveBat:
                case Monster.Skeleton:
                case Monster.BoneFighter:
                case Monster.AxeSkeleton:
                case Monster.BoneWarrior:
                case Monster.BoneElite:
                case Monster.Dung:
                case Monster.Dark:
                case Monster.WoomaSoldier:
                case Monster.WoomaFighter:
                case Monster.WoomaWarrior:
                case Monster.FlamingWooma:
                case Monster.WoomaGuardian:
                case Monster.WoomaTaurus:
                case Monster.WhimperingBee:
                case Monster.GiantWorm:
                case Monster.Centipede:
                case Monster.BlackMaggot:
                case Monster.Tongs:
                case Monster.EvilTongs:
                case Monster.BugBat:
                case Monster.WedgeMoth:
                case Monster.RedBoar:
                case Monster.BlackBoar:
                case Monster.SnakeScorpion:
                case Monster.WhiteBoar:
                case Monster.EvilSnake:
                case Monster.SpiderBat:
                case Monster.VenomSpider:
                case Monster.GangSpider:
                case Monster.GreatSpider:
                case Monster.LureSpider:
                case Monster.BigApe:
                case Monster.EvilApe:
                case Monster.GrayEvilApe:
                case Monster.RedEvilApe:
                case Monster.BigRat:
                case Monster.ZumaArcher:
                case Monster.Ghoul:
                case Monster.KingHog:
                case Monster.Shinsu1:
                case Monster.SpiderFrog:
                case Monster.HoroBlaster:
                case Monster.BlueHoroBlaster:
                case Monster.KekTal:
                case Monster.VioletKekTal:
                case Monster.RoninGhoul:
                case Monster.ToxicGhoul:
                case Monster.BoneCaptain:
                case Monster.BoneSpearman:
                case Monster.BoneBlademan:
                case Monster.BoneArcher:
                case Monster.Minotaur:
                case Monster.IceMinotaur:
                case Monster.ElectricMinotaur:
                case Monster.WindMinotaur:
                case Monster.FireMinotaur:
                case Monster.ShellNipper:
                case Monster.Keratoid:
                case Monster.GiantKeratoid:
                case Monster.SkyStinger:
                case Monster.SandWorm:
                case Monster.VisceralWorm:
                case Monster.RedSnake:
                case Monster.TigerSnake:
                case Monster.GiantWhiteSnake:
                case Monster.BlueSnake:
                case Monster.YellowSnake:
                case Monster.AxeOma:
                case Monster.SwordOma:
                case Monster.WingedOma:
                case Monster.FlailOma:
                case Monster.OmaGuard:
                case Monster.KatanaGuard:
                case Monster.RedFrogSpider:
                case Monster.BrownFrogSpider:
                case Monster.HalloweenScythe:
                case Monster.GhastlyLeecher:
                case Monster.CyanoGhast:
                case Monster.RedTurtle:
                case Monster.GreenTurtle:
                case Monster.BlueTurtle:
                case Monster.TowerTurtle:
                case Monster.DarkTurtle:
                case Monster.DarkSwordOma:
                case Monster.DarkAxeOma:
                case Monster.DarkWingedOma:
                case Monster.BoneWhoo:
                case Monster.DarkSpider:
                case Monster.ViscusWorm:
                case Monster.ViscusCrawler:
                case Monster.CrawlerLave:
                case Monster.DarkYob:
                case Monster.ValeBat:
                case Monster.Weaver:
                case Monster.VenomWeaver:
                case Monster.CrackingWeaver:
                case Monster.ArmingWeaver:
                case Monster.SpiderWarrior:
                case Monster.SpiderBarbarian:
                    Frames = FrameSet.Monsters[2];
                    break;
                case Monster.CannibalPlant:
                    Frames = FrameSet.Monsters[3];
                    break;
                case Monster.ForestYeti:
                case Monster.CaveMaggot:
                case Monster.FrostYeti:
                    Frames = FrameSet.Monsters[4];
                    break;
                case Monster.Scorpion:
                    Frames = FrameSet.Monsters[5];
                    break;
                case Monster.ChestnutTree:
                case Monster.EbonyTree:
                case Monster.LargeMushroom:
                case Monster.CherryTree:
                case Monster.ChristmasTree:
                case Monster.SnowTree:
                    Frames = FrameSet.Monsters[6];
                    break;
                case Monster.EvilCentipede:
                    Frames = FrameSet.Monsters[7];
                    break;
                case Monster.BugBatMaggot:
                    Frames = FrameSet.Monsters[8];
                    break;
                case Monster.CrystalSpider:
                case Monster.WhiteFoxman:
                case Monster.LightTurtle:
                case Monster.CrystalWeaver:
                    Frames = FrameSet.Monsters[9];
                    break;
                case Monster.RedMoonEvil:
                    Frames = FrameSet.Monsters[10];
                    break;
                case Monster.ZumaStatue:
                case Monster.ZumaGuardian:
                case Monster.FrozenZumaStatue:
                case Monster.FrozenZumaGuardian:
                    Stoned = info.Extra;
                    Frames = FrameSet.Monsters[11];
                    break;
                case Monster.ZumaTaurus:
                    Stoned = info.Extra;
                    Frames = FrameSet.Monsters[12];
                    break;
                case Monster.RedThunderZuma:
                case Monster.FrozenRedZuma:
                    Stoned = info.Extra;
                    Frames = FrameSet.Monsters[13];
                    break;
                case Monster.KingScorpion:
                case Monster.DarkDevil:
                case Monster.RightGuard:
                case Monster.LeftGuard:
                case Monster.MinotaurKing:
                    Frames = FrameSet.Monsters[14];
                    break;
                case Monster.BoneFamiliar:
                    Frames = FrameSet.Monsters[15];
                    if (!info.Extra) ActionFeed.Add(new QueuedAction { Action = MirAction.Appear, Direction = Direction, Location = CurrentLocation });
                    break;
                case Monster.Shinsu:
                    Frames = FrameSet.Monsters[16];
                    if (!info.Extra) ActionFeed.Add(new QueuedAction { Action = MirAction.Appear, Direction = Direction, Location = CurrentLocation });
                    break;
                case Monster.DigOutZombie:
                    Frames = FrameSet.Monsters[17];
                    break;
                case Monster.ClZombie:
                case Monster.NdZombie:
                case Monster.CrawlerZombie:
                    Frames = FrameSet.Monsters[18];
                    break;
                case Monster.ShamanZombie:
                    Frames = FrameSet.Monsters[19];
                    break;
                case Monster.Khazard:
                case Monster.FinialTurtle:
                    Frames = FrameSet.Monsters[20];
                    break;
                case Monster.BoneLord:
                    Frames = FrameSet.Monsters[21];
                    break;
                case Monster.FrostTiger:
                case Monster.FlameTiger:
                    SitDown = info.Extra;
                    Frames = FrameSet.Monsters[22];
                    break;
                case Monster.Yimoogi:
                case Monster.RedYimoogi:
                case Monster.Snake10:
                case Monster.Snake11:
                case Monster.Snake12:
                case Monster.Snake13:
                case Monster.Snake14:
                case Monster.Snake15:
                case Monster.Snake16:
                case Monster.Snake17:
                    Frames = FrameSet.Monsters[23];
                    break;
                case Monster.HolyDeva:
                    Frames = FrameSet.Monsters[24];
                    if (!info.Extra) ActionFeed.Add(new QueuedAction { Action = MirAction.Appear, Direction = Direction, Location = CurrentLocation });
                    break;
                case Monster.GreaterWeaver:
                case Monster.RootSpider:
                    Frames = FrameSet.Monsters[25];
                    break;
                case Monster.BombSpider:
                case Monster.MutatedHugger:
                    Frames = FrameSet.Monsters[26];
                    break;
                case Monster.CrossbowOma:
                case Monster.DarkCrossbowOma:
                    Frames = FrameSet.Monsters[27];
                    break;
                case Monster.YinDevilNode:
                case Monster.YangDevilNode:
                    Frames = FrameSet.Monsters[28];
                    break;
                case Monster.OmaKing:
                    Frames = FrameSet.Monsters[29];
                    break;
                case Monster.BlackFoxman:
                case Monster.RedFoxman:
                    Frames = FrameSet.Monsters[30];
                    break;
                case Monster.TrapRock:
                    Frames = FrameSet.Monsters[31];
                    break;
                case Monster.GuardianRock:
                    Frames = FrameSet.Monsters[32];
                    break;
                case Monster.ThunderElement:
                case Monster.CloudElement:
                    Frames = FrameSet.Monsters[33];
                    break;
                case Monster.GreatFoxSpirit:
                    Frames = FrameSet.Monsters[34 + Stage];
                    break;
                case Monster.HedgeKekTal:
                case Monster.BigHedgeKekTal:
                    Frames = FrameSet.Monsters[39];
                    break;
                case Monster.EvilMir:
                    Frames = FrameSet.Monsters[40];
                    break;
                case Monster.DragonStatue:
                    Frames = FrameSet.Monsters[41 + (byte)Direction];
                    break;
                case Monster.ArcherGuard:
                    Frames = FrameSet.Monsters[47];
                    break;
                case Monster.TaoistGuard:
                    Frames = FrameSet.Monsters[48];
                    break;
                case Monster.VampireSpider://SummonVampire
                    Frames = FrameSet.Monsters[49];
                    break;
                case Monster.SpittingToad://SummonToad
                    Frames = FrameSet.Monsters[50];
                    break;
                case Monster.SnakeTotem://SummonSnakes Totem
                    Frames = FrameSet.Monsters[51];
                    break;
                case Monster.CharmedSnake://SummonSnakes Snake
                    Frames = FrameSet.Monsters[52];
                    break;
                case Monster.HighAssassin:
                    Frames = FrameSet.Monsters[53];
                    break;
                case Monster.DarkDustPile:
                case Monster.MudPile:
                case Monster.Treasurebox:
                case Monster.SnowPile:
                    Frames = FrameSet.Monsters[54];
                    break;
                case Monster.Football:
                    Frames = FrameSet.Monsters[55];
                    break;
                case Monster.GingerBreadman:
                    Frames = FrameSet.Monsters[56];
                    break;
                case Monster.DreamDevourer:
                    Frames = FrameSet.Monsters[57];
                    break;
                case Monster.TailedLion:
                    Frames = FrameSet.Monsters[58];
                    break;
                case Monster.Behemoth:
                    Frames = FrameSet.Monsters[59];
                    break;
                case Monster.Hugger:
                case Monster.ManectricSlave:
                    Frames = FrameSet.Monsters[60];
                    break;
                case Monster.DarkDevourer:
                    Frames = FrameSet.Monsters[61];
                    break;
                case Monster.Snowman:
                    Frames = FrameSet.Monsters[62];
                    break;
                case Monster.GiantEgg:
                case Monster.IcePillar:
                    Frames = FrameSet.Monsters[63];
                    break;
                case Monster.BlueSanta:
                    Frames = FrameSet.Monsters[64];
                    break;
                case Monster.BattleStandard:
                    Frames = FrameSet.Monsters[65];
                    break;
                case Monster.WingedTigerLord:
                    Frames = FrameSet.Monsters[66];
                    break;
                case Monster.TurtleKing:
                    Frames = FrameSet.Monsters[67];
                    break;
                case Monster.Bush:
                    Frames = FrameSet.Monsters[68];
                    break;
                case Monster.HellSlasher:
                case Monster.HellCannibal:
                case Monster.ManectricClub:
                    Frames = FrameSet.Monsters[69];
                    break;
                case Monster.HellPirate:
                    Frames = FrameSet.Monsters[70];
                    break;
                case Monster.HellBolt:
                case Monster.WitchDoctor:
                    Frames = FrameSet.Monsters[71];
                    break;
                case Monster.HellKeeper:
                    Frames = FrameSet.Monsters[72];
                    break;
                case Monster.ManectricHammer:
                    Frames = FrameSet.Monsters[73];
                    break;
                case Monster.ManectricStaff:
                    Frames = FrameSet.Monsters[74];
                    break;
                case Monster.NamelessGhost:
                case Monster.DarkGhost:
                case Monster.ChaosGhost:
                case Monster.ManectricBlest:
                case Monster.TrollHammer:
                case Monster.TrollBomber:
                case Monster.TrollStoner:
                case Monster.MutatedManworm:
                case Monster.CrazyManworm:
                    Frames = FrameSet.Monsters[75];
                    break;
                case Monster.ManectricKing:
                case Monster.TrollKing:
                    Frames = FrameSet.Monsters[76];
                    break;
                case Monster.FlameSpear:
                case Monster.FlameMage:
                case Monster.FlameScythe:
                case Monster.FlameAssassin:
                    Frames = FrameSet.Monsters[77];
                    break;
                case Monster.FlameQueen:
                    Frames = FrameSet.Monsters[78];
                    break;
                case Monster.HellKnight1:
                case Monster.HellKnight2:
                case Monster.HellKnight3:
                case Monster.HellKnight4:
                    Frames = FrameSet.Monsters[79];
                    if (!info.Extra) ActionFeed.Add(new QueuedAction { Action = MirAction.Appear, Direction = Direction, Location = CurrentLocation });
                    break;
                case Monster.HellLord:
                    Frames = FrameSet.Monsters[80];
                    break;
                case Monster.WaterGuard:
                    Frames = FrameSet.Monsters[81];
                    break;
                case Monster.IceGuard:
                case Monster.ElementGuard:
                    Frames = FrameSet.Monsters[82];
                    break;
                case Monster.DemonGuard:
                    Frames = FrameSet.Monsters[83];
                    break;
                case Monster.KingGuard:
                    Frames = FrameSet.Monsters[84];
                    break;
                case Monster.Bunny2:
                case Monster.Bunny:
                    Frames = FrameSet.Monsters[85];
                    break;
                case Monster.DarkBeast:
                case Monster.LightBeast:
                    Frames = FrameSet.Monsters[86];
                    break;
                case Monster.HardenRhino:
                    Frames = FrameSet.Monsters[87];
                    break;
                case Monster.AncientBringer:
                    Frames = FrameSet.Monsters[88];
                    break;
                case Monster.Jar1:
                    Frames = FrameSet.Monsters[89];
                    break;
                case Monster.SeedingsGeneral:
                    Frames = FrameSet.Monsters[90];
                    break;
                case Monster.Tucson:
                case Monster.TucsonFighter:
                    Frames = FrameSet.Monsters[91];
                    break;
                case Monster.FrozenDoor:
                    Frames = FrameSet.Monsters[92];
                    break;
                case Monster.TucsonMage:
                    Frames = FrameSet.Monsters[93];
                    break;
                case Monster.TucsonWarrior:
                    Frames = FrameSet.Monsters[94];
                    break;
                case Monster.Armadillo:
                    Frames = FrameSet.Monsters[95];
                    break;
                case Monster.ArmadilloElder:
                    Frames = FrameSet.Monsters[96];
                    break;
                case Monster.TucsonEgg:
                    Frames = FrameSet.Monsters[97];
                    break;
                case Monster.PlaguedTucson:
                    Frames = FrameSet.Monsters[98];
                    break;
                case Monster.SandSnail:
                    Frames = FrameSet.Monsters[99];
                    break;
                case Monster.CannibalTentacles:
                    Frames = FrameSet.Monsters[100];
                    break;
                case Monster.TucsonGeneral:
                    Frames = FrameSet.Monsters[101];
                    break;
                case Monster.GasToad:
                    Frames = FrameSet.Monsters[102];
                    break;
                case Monster.Mantis:
                    Frames = FrameSet.Monsters[103];
                    break;
                case Monster.SwampWarrior:
                    Frames = FrameSet.Monsters[104];
                    break;
                case Monster.AssassinBird:
                    Frames = FrameSet.Monsters[105];
                    break;
                case Monster.RhinoWarrior:
                    Frames = FrameSet.Monsters[106];
                    break;
                case Monster.RhinoPriest:
                    Frames = FrameSet.Monsters[107];
                    break;
                case Monster.SwampSlime:
                    Frames = FrameSet.Monsters[108];
                    break;
                case Monster.RockGuard:
                    Frames = FrameSet.Monsters[109];
                    break;
                case Monster.MudWarrior:
                    Frames = FrameSet.Monsters[110];
                    break;
                case Monster.SmallPot:
                    Frames = FrameSet.Monsters[111];
                    break;
                case Monster.TreeQueen:
                    Frames = FrameSet.Monsters[112];
                    break;
                case Monster.ShellFighter:
                    Frames = FrameSet.Monsters[113];
                    break;
                case Monster.DarkBaboon:
                    Frames = FrameSet.Monsters[114];
                    break;
                case Monster.TwinHeadBeast:
                    Frames = FrameSet.Monsters[115];
                    break;
                case Monster.OmaCannibal:
                    Frames = FrameSet.Monsters[116];
                    break;
                case Monster.OmaBlest:
                    Frames = FrameSet.Monsters[121];
                    break;
                case Monster.OmaSlasher:
                    Frames = FrameSet.Monsters[117];
                    break;
                case Monster.OmaAssassin:
                    Frames = FrameSet.Monsters[118];
                    break;
                case Monster.OmaMage:
                    Frames = FrameSet.Monsters[119];
                    break;
                case Monster.OmaWitchDoctor:
                    Frames = FrameSet.Monsters[120];
                    break;
                case Monster.LightningBead:
                case Monster.HealingBead:
                case Monster.PowerUpBead:
                    Frames = FrameSet.Monsters[122];
                    break;
                case Monster.DarkOmaKing:
                    Frames = FrameSet.Monsters[123];
                    break;
                case Monster.CaveMage:
                    Frames = FrameSet.Monsters[124];
                    break;
                case Monster.Mandrill:
                    Frames = FrameSet.Monsters[125];
                    break;
                case Monster.PlagueCrab:
                    Frames = FrameSet.Monsters[126];
                    break;
                case Monster.CreeperPlant:
                    Frames = FrameSet.Monsters[127];
                    break;
                case Monster.SackWarrior:
                    Frames = FrameSet.Monsters[128];
                    break;
                case Monster.WereTiger:
                    Frames = FrameSet.Monsters[129];
                    break;
                case Monster.KingHydrax:
                    Frames = FrameSet.Monsters[130];
                    break;
                case Monster.FloatingWraith:
                    Frames = FrameSet.Monsters[131];
                    break;
                case Monster.ArmedPlant:
                    Frames = FrameSet.Monsters[132];
                    break;
                case Monster.AvengerPlant:
                    Frames = FrameSet.Monsters[133];
                    break;
                case Monster.Nadz:
                case Monster.AvengingSpirit:
                    Frames = FrameSet.Monsters[134];
                    break;
                case Monster.AvengingWarrior:
                    Frames = FrameSet.Monsters[135];
                    break;
                case Monster.AxePlant:
                case Monster.ClawBeast:
                    Frames = FrameSet.Monsters[136];
                    break;
                case Monster.WoodBox:
                    Frames = FrameSet.Monsters[137];
                    break;
                case Monster.KillerPlant:
                    Frames = FrameSet.Monsters[138];
                    break;
                case Monster.Hydrax:
                    Frames = FrameSet.Monsters[139];
                    break;
                case Monster.Basiloid:
                    Frames = FrameSet.Monsters[140];
                    break;
                case Monster.HornedMage:
                    Frames = FrameSet.Monsters[141];
                    break;
                case Monster.HornedArcher:
                case Monster.ColdArcher:
                    Frames = FrameSet.Monsters[142];
                    break;
                case Monster.HornedWarrior:
                    Frames = FrameSet.Monsters[143];
                    break;
                case Monster.FloatingRock:
                    Frames = FrameSet.Monsters[144];
                    break;
                case Monster.ScalyBeast:
                    Frames = FrameSet.Monsters[145];
                    break;
                case Monster.HornedSorceror:
                    Frames = FrameSet.Monsters[146];
                    break;
                case Monster.BoulderSpirit:
                    Frames = FrameSet.Monsters[147];
                    break;
                case Monster.HornedCommander:
                    Frames = FrameSet.Monsters[148];
                    break;
                case Monster.MoonStone:
                case Monster.SunStone:
                case Monster.LightningStone:
                    Frames = FrameSet.Monsters[149];
                    break;
                case Monster.Turtlegrass:
                    Frames = FrameSet.Monsters[150];
                    break;
                case Monster.Mantree:
                    Frames = FrameSet.Monsters[151];
                    break;
                case Monster.Bear:
                    Frames = FrameSet.Monsters[152];
                    break;
                case Monster.Leopard:
                    Frames = FrameSet.Monsters[153];
                    break;
                case Monster.ChieftainArcher:
                    Frames = FrameSet.Monsters[154];
                    break;
                case Monster.ChieftainSword:
                    Frames = FrameSet.Monsters[155];
                    break;
                case Monster.StoningSpider: //StoneTrap
                    Frames = FrameSet.Monsters[156];
                    break;
                case Monster.DarkSpirit:
                case Monster.FrozenSoldier:
                    Frames = FrameSet.Monsters[157];
                    break;
                case Monster.FrozenFighter:
                    Frames = FrameSet.Monsters[158];
                    break;
                case Monster.FrozenArcher:
                    Frames = FrameSet.Monsters[159];
                    break;
                case Monster.FrozenKnight:
                    Frames = FrameSet.Monsters[160];
                    break;
                case Monster.FrozenGolem:
                    Frames = FrameSet.Monsters[161];
                    break;
                case Monster.IcePhantom:
                    Frames = FrameSet.Monsters[162];
                    break;
                case Monster.SnowWolf:
                    Frames = FrameSet.Monsters[163];
                    break;
                case Monster.SnowWolfKing:
                    Frames = FrameSet.Monsters[164];
                    break;
                case Monster.WaterDragon:
                    Frames = FrameSet.Monsters[165];
                    break;
                case Monster.BlackTortoise:
                    Frames = FrameSet.Monsters[166];
                    break;
                case Monster.Manticore:
                    Frames = FrameSet.Monsters[167];
                    break;
                case Monster.DragonWarrior:
                    Frames = FrameSet.Monsters[168];
                    break;
                case Monster.DragonArcher:
                    Frames = FrameSet.Monsters[169];
                    break;
                case Monster.Kirin:
                    Frames = FrameSet.Monsters[170];
                    break;
                case Monster.Guard3:
                    Frames = FrameSet.Monsters[171];
                    break;
                case Monster.ArcherGuard3:
                    Frames = FrameSet.Monsters[172];
                    break;

                //173 blank

                case Monster.FrozenMiner:
                    Frames = FrameSet.Monsters[174];
                    break;
                case Monster.FrozenAxeman:
                    Frames = FrameSet.Monsters[175];
                    break;
                case Monster.FrozenMagician:
                    Frames = FrameSet.Monsters[176];
                    break;
                case Monster.SnowYeti:
                    Frames = FrameSet.Monsters[177];
                    break;
                case Monster.IceCrystalSoldier:
                    Frames = FrameSet.Monsters[178];
                    break;
                case Monster.DarkWraith:
                    Frames = FrameSet.Monsters[179];
                    break;
                case Monster.CrystalBeast:
                    Frames = FrameSet.Monsters[180];
                    break;
                case Monster.RedOrb:
                case Monster.BlueOrb:
                case Monster.YellowOrb:
                case Monster.GreenOrb:
                case Monster.WhiteOrb:
                    Frames = FrameSet.Monsters[181];
                    break;
                case Monster.FatalLotus:
                    Frames = FrameSet.Monsters[182];
                    break;
                case Monster.AntCommander:
                    Frames = FrameSet.Monsters[183];
                    break;
                case Monster.CargoBoxwithlogo:
                case Monster.CargoBox:
                    Frames = FrameSet.Monsters[184];
                    break;
                case Monster.Doe:
                    Frames = FrameSet.Monsters[185];
                    break;
                case Monster.AngryReindeer:
                    Frames = FrameSet.Monsters[186];
                    break;
                case Monster.DeathCrawler:
                    Frames = FrameSet.Monsters[187];
                    break;
                case Monster.UndeadWolf:
                    Frames = FrameSet.Monsters[188];
                    break;
                case Monster.BurningZombie:
                case Monster.FrozenZombie://
                    Frames = FrameSet.Monsters[189];
                    break;
                case Monster.MudZombie:
                    Frames = FrameSet.Monsters[190];
                    break;
                case Monster.BloodBaboon:
                    Frames = FrameSet.Monsters[191];
                    break;
                case Monster.PoisonHugger:
                    Frames = FrameSet.Monsters[192];
                    break;
                case Monster.FireCat:
                    Frames = FrameSet.Monsters[193];
                    break;
                case Monster.CatWidow:
                    Frames = FrameSet.Monsters[194];
                    break;
                case Monster.StainHammerCat:
                    Frames = FrameSet.Monsters[195];
                    break;
                case Monster.BlackHammerCat:
                    Frames = FrameSet.Monsters[196];
                    break;
                case Monster.StrayCat:
                    Frames = FrameSet.Monsters[197];
                    break;
                case Monster.CatShaman:
                    Frames = FrameSet.Monsters[198];
                    break;
                case Monster.Jar2:
                    Frames = FrameSet.Monsters[199];
                    break;
                case Monster.RestlessJar:
                    Frames = FrameSet.Monsters[200];
                    break;
                case Monster.FlamingMutant:
                case Monster.FlyingStatue:
                case Monster.ManectricClaw:
                    Frames = FrameSet.Monsters[201];
                    break;
                case Monster.StoningStatue:
                    Frames = FrameSet.Monsters[202];
                    break;
                case Monster.ArcherGuard2:
                    Frames = FrameSet.Monsters[203];
                    break;
                case Monster.Tornado:
                    Frames = FrameSet.Monsters[204];
                    break;

                case Monster.HellBomb1:
                    Frames = FrameSet.Monsters[205];
                    break;
                case Monster.HellBomb2:
                    Frames = FrameSet.Monsters[206];
                    break;
                case Monster.HellBomb3:
                    Frames = FrameSet.Monsters[207];
                    break;


                case Monster.BabyPig:
                case Monster.Chick:
                case Monster.Kitten:
                case Monster.BabySkeleton:
                case Monster.Baekdon:
                case Monster.Wimaen:
                case Monster.BlackKitten:
                case Monster.BabyDragon:
                case Monster.OlympicFlame:
                case Monster.BabySnowMan:
                case Monster.Frog:
                case Monster.BabyMonkey:
                case Monster.AngryBird:
                case Monster.Foxey:
                    Frames = FrameSet.HelperPets[((ushort)BaseImage) - 10000];
                    break;
                case Monster.SabukGate:
                    Frames = FrameSet.Gates[0];
                    break;
                case Monster.GiGateSouth:
                    Frames = FrameSet.Gates[1];
                    break;
                case Monster.GiGateEast:
                    Frames = FrameSet.Gates[2];
                    break;
                case Monster.GiGateWest:
                    Frames = FrameSet.Gates[3];
                    break;
                case Monster.PalaceWallLeft:
                    Frames = FrameSet.Walls[0];
                    break;
                case Monster.PalaceWall1:
                    Frames = FrameSet.Walls[1];
                    break;
                case Monster.PalaceWall2:
                    Frames = FrameSet.Walls[2];
                    break;
                case Monster.SSabukWall1:
                    Frames = FrameSet.Walls[3];
                    break;
                case Monster.SSabukWall2:
                    Frames = FrameSet.Walls[4];
                    break;
                case Monster.SSabukWall3:
                    Frames = FrameSet.Walls[5];
                    break;
                default:
                    Frames = FrameSet.Monsters[0];
                    break;
            }
            
        }

        //死循环调用入口
        public override void Process()
        {
            bool update = CMain.Time >= NextMotion || GameScene.CanMove;
            SkipFrames = ActionFeed.Count > 1;

            ProcessFrames();

            if (Frame == null)
            {
                DrawFrame = 0;
                DrawWingFrame = 0;
            }
            else 
            {
                DrawFrame = Frame.Start + (Frame.OffSet * (byte)Direction) + FrameIndex;
                if (Frame.EffectCount > 0)
                {
                    DrawWingFrame = Frame.EffectStart + (Frame.EffectOffSet * (byte)Direction) + EffectFrameIndex;
                }
            }


            #region Moving OffSet

            switch (CurrentAction)
            {
                case MirAction.Walking:
                case MirAction.Running:
                case MirAction.Pushed:
                case MirAction.DashL:
                case MirAction.DashR:
                    if (Frame == null)
                    {
                        OffSetMove = Point.Empty;
                        Movement = CurrentLocation;
                        break;
                    }
                    int i = CurrentAction == MirAction.Running ? 2 : 1;
                    //有可能跑3格？
                    if (CurrentAction == MirAction.Running && BaseImage== Monster.Monster453)
                    {
                        i = 3;
                    }
                    Movement = Functions.PointMove(CurrentLocation, Direction, CurrentAction == MirAction.Pushed ? 0 : -i);
                    
                    int count = Frame.Count;
                    int index = FrameIndex;

                    if (CurrentAction == MirAction.DashR || CurrentAction == MirAction.DashL)
                    {
                        count = 3;
                        index %= 3;
                    }

                    switch (Direction)
                    {
                        case MirDirection.Up:
                            OffSetMove = new Point(0, (int)((MapControl.CellHeight * i / (float)(count)) * (index + 1)));
                            break;
                        case MirDirection.UpRight:
                            OffSetMove = new Point((int)((-MapControl.CellWidth * i / (float)(count)) * (index + 1)), (int)((MapControl.CellHeight * i / (float)(count)) * (index + 1)));
                            break;
                        case MirDirection.Right:
                            OffSetMove = new Point((int)((-MapControl.CellWidth * i / (float)(count)) * (index + 1)), 0);
                            break;
                        case MirDirection.DownRight:
                            OffSetMove = new Point((int)((-MapControl.CellWidth * i / (float)(count)) * (index + 1)), (int)((-MapControl.CellHeight * i / (float)(count)) * (index + 1)));
                            break;
                        case MirDirection.Down:
                            OffSetMove = new Point(0, (int)((-MapControl.CellHeight * i / (float)(count)) * (index + 1)));
                            break;
                        case MirDirection.DownLeft:
                            OffSetMove = new Point((int)((MapControl.CellWidth * i / (float)(count)) * (index + 1)), (int)((-MapControl.CellHeight * i / (float)(count)) * (index + 1)));
                            break;
                        case MirDirection.Left:
                            OffSetMove = new Point((int)((MapControl.CellWidth * i / (float)(count)) * (index + 1)), 0);
                            break;
                        case MirDirection.UpLeft:
                            OffSetMove = new Point((int)((MapControl.CellWidth * i / (float)(count)) * (index + 1)), (int)((MapControl.CellHeight * i / (float)(count)) * (index + 1)));
                            break;
                    }

                    OffSetMove = new Point(OffSetMove.X % 2 + OffSetMove.X, OffSetMove.Y % 2 + OffSetMove.Y);
                    break;
                default:
                    OffSetMove = Point.Empty;
                    Movement = CurrentLocation;
                    break;
            }

            #endregion


            DrawY = Movement.Y > CurrentLocation.Y ? Movement.Y : CurrentLocation.Y;

            DrawLocation = new Point((Movement.X - User.Movement.X + MapControl.OffSetX) * MapControl.CellWidth, (Movement.Y - User.Movement.Y + MapControl.OffSetY) * MapControl.CellHeight);
            DrawLocation.Offset(-OffSetMove.X, -OffSetMove.Y);
            DrawLocation.Offset(User.OffSetMove);
            DrawLocation = DrawLocation.Add(ManualLocationOffset);
            DrawLocation.Offset(GlobalDisplayLocationOffset);

            if (BodyLibrary != null && update)
            {
                FinalDrawLocation = DrawLocation.Add(BodyLibrary.GetOffSet(DrawFrame));
                DisplayRectangle = new Rectangle(DrawLocation, BodyLibrary.GetTrueSize(DrawFrame));
            }

            for (int i = 0; i < Effects.Count; i++)
                Effects[i].Process();

            Color colour = DrawColour;
            if (Poison == PoisonType.None)
                DrawColour = Color.White;
            if (Poison.HasFlag(PoisonType.Green))
                DrawColour = Color.Green;
            if (Poison.HasFlag(PoisonType.Red))
                DrawColour = Color.Red;
            if (Poison.HasFlag(PoisonType.Bleeding))
                DrawColour = Color.DarkRed;
            if (Poison.HasFlag(PoisonType.Slow))
                DrawColour = Color.Purple;
            if (Poison.HasFlag(PoisonType.Stun))
                DrawColour = Color.Yellow;
            if (Poison.HasFlag(PoisonType.Frozen))
                DrawColour = Color.Blue;
            if (Poison.HasFlag(PoisonType.Paralysis) || Poison.HasFlag(PoisonType.LRParalysis))
                DrawColour = Color.Gray;
            if (Poison.HasFlag(PoisonType.DelayedExplosion))
                DrawColour = Color.Orange;
            if (Poison.HasFlag(PoisonType.DelayedBomb))
                DrawColour = Color.Orange;
            if (colour != DrawColour) GameScene.Scene.MapControl.TextureValid = false;
        }


        //这里添加的是动作特效，和动作捆绑
        //比如攻击特效，一般都在这里加
        //每个动作调用一次
        public bool SetAction()
        {
            if (NextAction != null && !GameScene.CanMove)
            {
                switch (NextAction.Action)
                {
                    case MirAction.Walking:
                    case MirAction.Running:
                    case MirAction.Pushed:
                        return false;
                }
            }

            //IntelligentCreature
            switch (BaseImage)
            {
                case Monster.BabyPig:
                case Monster.Chick:
                case Monster.Kitten:
                case Monster.BabySkeleton:
                case Monster.Baekdon:
                case Monster.Wimaen:
                case Monster.BlackKitten:
                case Monster.BabyDragon:
                case Monster.OlympicFlame:
                case Monster.BabySnowMan:
                case Monster.Frog:
                case Monster.BabyMonkey:
                case Monster.AngryBird:
                case Monster.Foxey:
                    BodyLibrary = Libraries.Pets[((ushort)BaseImage) - 10000];
                    break;
                    break;
            }

            bool nextClear = false;
            if (Frame != null && Frame.Eff1Clear)
            {
                nextClear = true;
            }
            //如果没有未处理的动作，默认就是站立的动作
            if (ActionFeed.Count == 0)
            {
                CurrentAction = Stoned ? MirAction.Stoned : MirAction.Standing;
                if (CurrentAction == MirAction.Standing)
                {
                    CurrentAction = SitDown ? MirAction.SitDown : MirAction.Standing;
                }

    

                Frames.Frames.TryGetValue(CurrentAction, out Frame);

                if (MapLocation != CurrentLocation)
                {
                    GameScene.Scene.MapControl.RemoveObject(this);
                    MapLocation = CurrentLocation;
                    GameScene.Scene.MapControl.AddObject(this);
                }

                FrameIndex = 0;
                EffectFrameIndex = 0;
                if (Frame == null) return false;

                FrameInterval = Frame.Interval;
                EffectFrameInterval = Frame.EffectInterval;

                //放在Frame中的特效在这里加入
                if (Frame.EffectCount > 0 && BodyLibrary != null)
                {
                    if (nextClear)
                    {
                        Effects.Clear();
                    }
                    Effects.Add(new Effect(BodyLibrary, Frame.EffectStart + (Frame.EffectOffSet * (byte)Direction), Frame.EffectCount, Frame.EffectCount * Frame.EffectInterval, this, CMain.Time + Frame.EffectStartTime));
                }
                //这个是第2组特效
                if (Frame.ECount2 > 0 && BodyLibrary != null)
                {
                    Effects.Clear();
                    Effects.Add(new Effect(BodyLibrary, Frame.EStart2 + (Frame.EOffSet2 * (byte)Direction), Frame.ECount2, Frame.ECount2 * Frame.EInterval2, this, CMain.Time + Frame.ETime2));
                }
            }
            else
            {
                QueuedAction action = ActionFeed[0];
                ActionFeed.RemoveAt(0);

                CurrentAction = action.Action;
                CurrentLocation = action.Location;
                Direction = action.Direction;

                Point temp;
                switch (CurrentAction)
                {
                    case MirAction.Walking:
                    case MirAction.Running:
                    case MirAction.Pushed:
                        int i = CurrentAction == MirAction.Running ? 2 : 1;
                        //有可能跑3格？
                        if (CurrentAction == MirAction.Running && BaseImage == Monster.Monster453)
                        {
                            i = 3;
                        }
                        temp = Functions.PointMove(CurrentLocation, Direction, CurrentAction == MirAction.Pushed ? 0 : -i);
                        break;
                    default:
                        temp = CurrentLocation;
                        break;
                }

                temp = new Point(action.Location.X, temp.Y > CurrentLocation.Y ? temp.Y : CurrentLocation.Y);

                if (MapLocation != temp)
                {
                    GameScene.Scene.MapControl.RemoveObject(this);
                    MapLocation = temp;
                    GameScene.Scene.MapControl.AddObject(this);
                }


                switch (CurrentAction)
                {
                    case MirAction.Pushed:
                        Frames.Frames.TryGetValue(MirAction.Walking, out Frame);
                        break;
                    case MirAction.AttackRange1:
                        if (!Frames.Frames.TryGetValue(CurrentAction, out Frame))
                            Frames.Frames.TryGetValue(MirAction.Attack1, out Frame);
                        break;
                    case MirAction.AttackRange2:
                        if (!Frames.Frames.TryGetValue(CurrentAction, out Frame))
                            Frames.Frames.TryGetValue(MirAction.Attack2, out Frame);
                        break;
                    case MirAction.AttackRange3:
                        if (!Frames.Frames.TryGetValue(CurrentAction, out Frame))
                            Frames.Frames.TryGetValue(MirAction.Attack3, out Frame);
                        break;
                    case MirAction.Special:
                        if (!Frames.Frames.TryGetValue(CurrentAction, out Frame))
                            Frames.Frames.TryGetValue(MirAction.Attack1, out Frame);
                        break;
                    case MirAction.Skeleton:
                        if (!Frames.Frames.TryGetValue(CurrentAction, out Frame))
                            Frames.Frames.TryGetValue(MirAction.Dead, out Frame);
                        break;
                    case MirAction.Hide:
                        switch (BaseImage)
                        {
                            case Monster.Shinsu1:
                                BodyLibrary = Libraries.Monsters[(ushort)Monster.Shinsu];
                                BaseImage = Monster.Shinsu;
                                BaseSound = (ushort)BaseImage * 10;
                                Frames = FrameSet.Monsters[16];
                                Frames.Frames.TryGetValue(CurrentAction, out Frame);
                                break;
                            default:
                                Frames.Frames.TryGetValue(CurrentAction, out Frame);
                                break;
                        }
                        break;
                    case MirAction.Dead:
                        switch (BaseImage)
                        {
                            case Monster.Shinsu:
                            case Monster.Shinsu1:
                            case Monster.HolyDeva:
                            case Monster.GuardianRock:
                            case Monster.CharmedSnake://SummonSnakes
                            case Monster.HellKnight1:
                            case Monster.HellKnight2:
                            case Monster.HellKnight3:
                            case Monster.HellKnight4:
                            case Monster.HellBomb1:
                            case Monster.HellBomb2:
                            case Monster.HellBomb3:
                                Remove();
                                return false;
                            default:
                                Frames.Frames.TryGetValue(CurrentAction, out Frame);
                                break;
                        }
                        break;
                    default:
                        Frames.Frames.TryGetValue(CurrentAction, out Frame);
                        break;

                }

                FrameIndex = 0;
                EffectFrameIndex = 0;
                if (Frame == null) return false;

                FrameInterval = Frame.Interval;
                EffectFrameInterval = Frame.EffectInterval;

                //放在Frame中的特效在这里加入
                if (Frame.EffectCount > 0 && BodyLibrary != null)
                {
                    if (nextClear)
                    {
                        Effects.Clear();
                    }
                    Effects.Add(new Effect(BodyLibrary, Frame.EffectStart + (Frame.EffectOffSet * (byte)Direction), Frame.EffectCount, Frame.EffectCount * Frame.EffectInterval, this,CMain.Time + Frame.EffectStartTime));
                }
                //这个是第2组特效
                if (Frame.ECount2 > 0 && BodyLibrary != null)
                {
                    Effects.Add(new Effect(BodyLibrary, Frame.EStart2 + (Frame.EOffSet2 * (byte)Direction), Frame.ECount2, Frame.ECount2 * Frame.EInterval2, this, CMain.Time + Frame.ETime2));
                }


                switch (CurrentAction)
                {
                    case MirAction.Appear:
                        PlaySummonSound();
                        switch (BaseImage)
                        {
                            case Monster.HellKnight1:
                            case Monster.HellKnight2:
                            case Monster.HellKnight3:
                            case Monster.HellKnight4:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)BaseImage], 448, 10, 600, this));
                                break;
                        }
                        break;
                    case MirAction.Show:
                        PlayPopupSound();
                        break;
                    case MirAction.Pushed:
                        FrameIndex = Frame.Count - 1;
                        GameScene.Scene.Redraw();
                        break;
                    case MirAction.Walking:
                    case MirAction.Running:
                        GameScene.Scene.Redraw();
                        break;
                    case MirAction.Attack1:
                        PlayAttackSound();
                        switch (BaseImage)
                        {
                            case Monster.FlamingWooma:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FlamingWooma], 224 + (int)Direction * 7, 7, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.ZumaTaurus:
                                if (CurrentAction == MirAction.Attack1)
                                    Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ZumaTaurus], 244 + (int)Direction * 8, 8, 8 * FrameInterval, this));
                                break;
                            case Monster.MinotaurKing:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.MinotaurKing], 272 + (int)Direction * 6, 6, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.FlamingMutant:///////////////////////////stupple 
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FlamingMutant], 304 + (int)Direction * 6, 6, Frame.Count * Frame.Interval, this));
                                break;
         
                            case Monster.DarkBeast:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.DarkBeast], 296 + (int)Direction * 4, 4, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.AncientBringer://丹墨攻击1，身前的地板砸效果
                                MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.AncientBringer], 560, 8, 400, Functions.PointMove(CurrentLocation, Direction, 1), CMain.Time + 500));
                                break;
              
                                
                            case Monster.OmaWitchDoctor://奥玛巫医攻击1，身前的地板砸效果
                                MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.OmaWitchDoctor], 856, 10, 500, Functions.PointMove(CurrentLocation, Direction, 2), CMain.Time + 500));
                                break;
                            case Monster.IceCrystalSoldier://冰晶战士攻击1，身前的地板砸效果
                                MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.IceCrystalSoldier], 464, 6, 600, Functions.PointMove(CurrentLocation, Direction, 1), CMain.Time + 500));
                                break;
                            case Monster.DemonGuard:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.DemonGuard], 288 + (int)Direction * 2, 2, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.Bear:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Bear], 321 + (int)Direction * 4, 4, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.Manticore:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Manticore], 505 + (int)Direction * 3, 3, Frame.Count * Frame.Interval, this));
                                break;
                          
                            case Monster.BlackHammerCat:
                                //Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.BlackHammerCat], 648 + (int)Direction * 11, 11, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.SeedingsGeneral:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.SeedingsGeneral], 536 + (int)Direction * 4, 4, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.TucsonMage:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.TucsonMage], 272 + (int)Direction * 4, 4, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.YinDevilNode:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.YinDevilNode], 26, 26, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.YangDevilNode:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.YangDevilNode], 26, 26, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.GreatFoxSpirit:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.GreatFoxSpirit], 355, 20, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.Jar1:
                                //Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Jar1], 130 + (int)Direction * 3, 3, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.Jar2:
                                //Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Jar2], 392 + (int)Direction * 6, 6, Frame.Count * Frame.Interval, this));
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Jar2], 440 + (int)Direction * 10, 10, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.RestlessJar:
                                
                               
                                break;
                            case Monster.EvilMir:
                                Effects.Add(new Effect(Libraries.Dragon, 60, 8, 8 * Frame.Interval, this));
                                Effects.Add(new Effect(Libraries.Dragon, 68, 14, 14 * Frame.Interval, this));
                                byte random = (byte)CMain.Random.Next(7);
                                for (int i = 0; i <= 7 + random; i++)
                                {
                                    Point source = new Point(User.CurrentLocation.X + CMain.Random.Next(-7, 7), User.CurrentLocation.Y + CMain.Random.Next(-7, 7));

                                    MapControl.Effects.Add(new Effect(Libraries.Dragon, 230 + (CMain.Random.Next(5) * 10), 5, 400, source, CMain.Time + CMain.Random.Next(1000)));
                                }
                                break;
                            case Monster.CrawlerLave:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.CrawlerLave], 224 + (int)Direction * 6, 6, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.HellKeeper:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.HellKeeper], 32, 8, 8 * Frame.Interval, this));
                                break;
                            case Monster.PoisonHugger:
                                User.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.PoisonHugger], 224, 5, 5 * Frame.Interval, User, 500, false));
                                break;
                            case Monster.IcePillar:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.IcePillar], 12, 6, 6 * 100, this));
                                break;
                            case Monster.TrollKing:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.TrollKing], 288, 6, 6 * Frame.Interval, this));
                                break;
                            case Monster.HellBomb1:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.HellLord], 61, 7, 7 * Frame.Interval, this));
                                break;
                            case Monster.HellBomb2:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.HellLord], 79, 9, 9 * Frame.Interval, this));
                                break;
                            case Monster.HellBomb3:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.HellLord], 97, 8, 8 * Frame.Interval, this));
                                break;
                        }
                        break;
                    case MirAction.Attack2:
                        PlaySecondAttackSound();
                        switch (BaseImage)
                        {
                            case Monster.CrystalSpider:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.CrystalSpider], 272 + (int)Direction * 10, 10, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.Yimoogi:
                            case Monster.RedYimoogi:
                            case Monster.Snake10:
                            case Monster.Snake11:
                            case Monster.Snake12:
                            case Monster.Snake13:
                            case Monster.Snake14:
                            case Monster.Snake15:
                            case Monster.Snake16:
                            case Monster.Snake17:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)BaseImage], 304, 6, Frame.Count * Frame.Interval, this));
                                Effects.Add(new Effect(Libraries.Magic2, 1280, 10, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.HellCannibal:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.HellCannibal], 310 + (int)Direction * 12, 12, 12 * Frame.Interval, this));
                                break;
                            case Monster.ManectricKing:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ManectricKing], 640 + (int)Direction * 10, 10, 10 * 100, this));
                                break;
                            case Monster.Jar2:
                                //Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Jar2], 544 + (int)Direction * 10, 10, 500, this));
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Jar2], 624 + (int)Direction * 8, 8, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.RestlessJar:
                             
                                break;
                            case Monster.AncientBringer://丹墨攻击2，狮子叫
                                //MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.AncientBringer], 740, 14, 1400, Functions.PointMove(CurrentLocation, Direction, 3), CMain.Time + 1000));
                                break;
                            case Monster.FrozenKnight://雪原勇士攻击2，身前的2格地板砸效果
                                MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FrozenKnight], 456, 10, 500, Functions.PointMove(CurrentLocation, Direction, 2), CMain.Time + 500));
                                break;
                            case Monster.IceCrystalSoldier://冰晶战士攻击2，身前的地板砸效果
                                MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.IceCrystalSoldier], 470, 6, 600, Functions.PointMove(CurrentLocation, Direction, 1), CMain.Time + 500));
                                break;
                            case Monster.Monster408://冰宫护卫攻击2，身前的地板砸效果
                                MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster408], 357, 6, 600, Functions.PointMove(CurrentLocation, Direction, 1), CMain.Time + 500));
                                break;
                                
                        }

                        if ((ushort)BaseImage >= 10000)
                        {
                            PlayPetSound();
                        }
                        break;
                    case MirAction.Attack3:
                        PlayThirdAttackSound();
                        switch (BaseImage)
                        {
                            case Monster.Yimoogi:
                            case Monster.RedYimoogi:
                            case Monster.Snake10:
                            case Monster.Snake11:
                            case Monster.Snake12:
                            case Monster.Snake13:
                            case Monster.Snake14:
                            case Monster.Snake15:
                            case Monster.Snake16:
                            case Monster.Snake17:
                                Effects.Add(new Effect(Libraries.Magic2, 1330, 10, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.Behemoth:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Behemoth], 697 + (int)Direction * 7, 7, Frame.Count * Frame.Interval, this));
                                break;
                        }
                        break;
                    case MirAction.Attack4:                   
                        break;
                    case MirAction.AttackRange1:
                        PlayRangeSound();
                        switch (BaseImage)
                        {
                            case Monster.KingScorpion:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.KingScorpion], 272 + (int)Direction * 8, 8, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.DarkDevil:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.DarkDevil], 272 + (int)Direction * 8, 8, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.ShamanZombie:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ShamanZombie], 232 + (int)Direction * 12, 6, Frame.Count * Frame.Interval, this));
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ShamanZombie], 328, 12, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.GuardianRock:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.GuardianRock], 12, 10, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.GreatFoxSpirit:
                                byte random = (byte)CMain.Random.Next(4);
                                for (int i = 0; i <= 4 + random; i++)
                                {
                                    Point source = new Point(User.CurrentLocation.X + CMain.Random.Next(-7, 7), User.CurrentLocation.Y + CMain.Random.Next(-7, 7));

                                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.GreatFoxSpirit], 375 + (CMain.Random.Next(3) * 20), 20, 1400, source, CMain.Time + CMain.Random.Next(600)));
                                }
                                break;
                 
                                
                            case Monster.EvilMir:
                                Effects.Add(new Effect(Libraries.Dragon, 90 + (int)Direction * 10, 10, 10 * Frame.Interval, this));
                                break;
                            case Monster.DragonStatue:
                                Effects.Add(new Effect(Libraries.Dragon, 310 + ((int)Direction / 3) * 20, 10, 10 * Frame.Interval, this));
                                break;
                            case Monster.TurtleKing:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.TurtleKing], 946, 10, Frame.Count * Frame.Interval, User));
                                break;
                            case Monster.FlyingStatue:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FlyingStatue], 314, 6, 6 * Frame.Interval, this));
                                //Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FlyingStatue], 329, 5, 5 * Frame.Interval, this)); this should follow the projectile
                                break;
                            case Monster.HellBolt:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.HellBolt], 304, 11, 11 * 100, this));
                                break;
                            case Monster.WitchDoctor:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.WitchDoctor], 304, 9, 9 * 100, this));
                                break;
                            case Monster.DarkDevourer:
                                User.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.DarkDevourer], 480, 7, 7 * Frame.Interval, User));
                                break;
                            case Monster.DreamDevourer:
                                User.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.DreamDevourer], 264, 7, 7 * Frame.Interval, User));
                                break;
                            case Monster.ManectricKing:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ManectricKing], 720, 12, 12 * 100, this));
                                break;
                            case Monster.IcePillar:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.IcePillar], 26, 6, 8 * 100, this) { Start = CMain.Time + 750 });
                                break;
                            case Monster.Monster434://昆仑叛军法师
                                Point source434 = (Point)action.Params[1];
                                if (source434 != null)
                                {
                                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster434], 469, 23, 2500, source434, CMain.Time + 100));
                                }
                                break;
                        }

                        TargetID = (uint)action.Params[0];
      
                        break;
                    case MirAction.AttackRange2:
                        PlaySecondRangeSound();
                        TargetID = (uint)action.Params[0];
            
                        switch (BaseImage)
                        {
                            case Monster.TurtleKing:
                                byte random = (byte)CMain.Random.Next(4);
                                for (int i = 0; i <= 4 + random; i++)
                                {
                                    Point source = new Point(User.CurrentLocation.X + CMain.Random.Next(-7, 7), User.CurrentLocation.Y + CMain.Random.Next(-7, 7));

                                    Effect ef = new Effect(Libraries.Monsters[(ushort)Monster.TurtleKing], CMain.Random.Next(2) == 0 ? 922 : 934, 12, 1200, source, CMain.Time + CMain.Random.Next(600));
                                    ef.Played += (o, e) => SoundManager.PlaySound(20000 + (ushort)Spell.HellFire * 10 + 1);
                                    MapControl.Effects.Add(ef);
                                }
                                break;
                            case Monster.Monster434://昆仑叛军法师
                                Point source434 = (Point)action.Params[1];
                                if (source434 != null)
                                {
                                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster434], 492, 18, 2500, source434, CMain.Time + 100));
                                }
                                break;
                            case Monster.Monster443:// 昆仑叛军道尊 小BOSS,毒云
                                Point source443 = (Point)action.Params[1];
                                if (source443 != null)
                                {
                                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster443], 502, 16, 3200, source443, CMain.Time + 100));
                                }
                                break;
                        }
                        break;
                    case MirAction.AttackRange3:
                        PlayThirdRangeSound();
                        TargetID = (uint)action.Params[0];
                        switch (BaseImage)
                        {
                            case Monster.TurtleKing:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.TurtleKing], 946, 10, Frame.Count * Frame.Interval, User));
                                break;
                            case Monster.Monster442://昆仑叛军箭神 小BOSS
                                Point source442 = (Point)action.Params[1];
                                if (source442 != null)
                                {
                                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster442], 516, 10, 1000, source442, CMain.Time + 500));
                                }
                                break;
                        }
                        break;
                    case MirAction.Struck:
                        uint attackerID = (uint)action.Params[0];
                        StruckWeapon = -2;
                        for (int i = 0; i < MapControl.Objects.Count; i++)
                        {
                            MapObject ob = MapControl.Objects[i];
                            if (ob.ObjectID != attackerID) continue;
                            if (ob.Race != ObjectType.Player) break;
                            PlayerObject player = ((PlayerObject)ob);
                            StruckWeapon = player.Weapon;
                            if (player.Class != MirClass.Assassin || StruckWeapon == -1) break; //Archer?
                            StruckWeapon = 1;
                            break;
                        }
                        PlayFlinchSound();
                        PlayStruckSound();
                        break;
                    case MirAction.Die:
                        switch (BaseImage)
                        {
                            case Monster.ManectricKing:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ManectricKing], 504 + (int)Direction * 9, 9, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.DarkDevil:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.DarkDevil], 336, 6, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.ShamanZombie:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ShamanZombie], 224, 8, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.RoninGhoul:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.RoninGhoul], 224, 10, Frame.Count * FrameInterval, this));
                                break;
                            case Monster.BoneCaptain:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.BoneCaptain], 224 + (int)Direction * 10, 10, Frame.Count * FrameInterval, this));
                                break;
                            case Monster.RightGuard:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.RightGuard], 296, 5, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.LeftGuard:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.LeftGuard], 296 + (int)Direction * 5, 5, 5 * Frame.Interval, this));
                                break;
                            case Monster.FrostTiger:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FrostTiger], 304, 10, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.Yimoogi:
                            case Monster.RedYimoogi:
                            case Monster.Snake10:
                            case Monster.Snake11:
                            case Monster.Snake12:
                            case Monster.Snake13:
                            case Monster.Snake14:
                            case Monster.Snake15:
                            case Monster.Snake16:
                            case Monster.Snake17:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Yimoogi], 352, 10, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.YinDevilNode:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.YinDevilNode], 52, 20, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.YangDevilNode:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.YangDevilNode], 52, 20, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.BlackFoxman:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.BlackFoxman], 224, 10, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.VampireSpider:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.VampireSpider], 296, 5, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.CharmedSnake:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.CharmedSnake], 40, 8, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.Manticore:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Manticore], 592, 9, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.ValeBat:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ValeBat], 224, 20, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.SpiderBat:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.SpiderBat], 224, 20, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.VenomWeaver:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.VenomWeaver], 224, 6, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.HellBolt:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.HellBolt], 325, 10, Frame.Count * Frame.Interval, this));
                                break;
                            case Monster.SabukGate:
                                Effects.Add(new Effect(Libraries.Effect, 136, 7, Frame.Count * Frame.Interval, this) { Light = -1 });
                                break;
                            case Monster.WingedTigerLord:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.WingedTigerLord], 650 + (int)Direction * 5, 5, Frame.Count * FrameInterval, this));
                                break;
                            case Monster.HellKnight1:
                            case Monster.HellKnight2:
                            case Monster.HellKnight3:
                            case Monster.HellKnight4:
                                Effects.Add(new Effect(Libraries.Monsters[(ushort)BaseImage], 448, 10, 600, this));
                                break;
                        }
                        PlayDieSound();
                        break;
                    case MirAction.Dead:
                        GameScene.Scene.Redraw();
                        GameScene.Scene.MapControl.SortObject(this);
                        if (MouseObject == this) MouseObject = null;
                        if (TargetObject == this) TargetObject = null;
                        if (MagicObject == this) MagicObject = null;

                        for (int i = 0; i < Effects.Count; i++)
                            Effects[i].Remove();

                        DeadTime = CMain.Time;

                        break;
                }

            }

            GameScene.Scene.MapControl.TextureValid = false;

            NextMotion = CMain.Time + FrameInterval;
            NextMotion2 = CMain.Time + EffectFrameInterval;

            return true;
        }

        public void SetCurrentEffects()
        {
            //BindingShot
            if (BindingShotCenter && ShockTime > CMain.Time)
            {
                int effectid = TrackableEffect.GetOwnerEffectID(ObjectID);
                if (effectid >= 0)
                    TrackableEffect.effectlist[effectid].RemoveNoComplete();

                TrackableEffect NetDropped = new TrackableEffect(new Effect(Libraries.MagicC, 7, 1, 1000, this) { Repeat = true, RepeatUntil = (ShockTime - 1500) });
                NetDropped.Complete += (o1, e1) =>
                {
                    SoundManager.PlaySound(20000 + 130 * 10 + 6);//sound M130-6
                    Effects.Add(new TrackableEffect(new Effect(Libraries.MagicC, 8, 8, 700, this)));
                };
                Effects.Add(NetDropped);
            }
            else if (BindingShotCenter && ShockTime <= CMain.Time)
            {
                int effectid = TrackableEffect.GetOwnerEffectID(ObjectID);
                if (effectid >= 0)
                    TrackableEffect.effectlist[effectid].Remove();

                //SoundManager.PlaySound(20000 + 130 * 10 + 6);//sound M130-6
                //Effects.Add(new TrackableEffect(new Effect(Libraries.ArcherMagic, 8, 8, 700, this)));

                ShockTime = 0;
                BindingShotCenter = false;
            }

        }

        //死循环处理帧动画
        private void ProcessFrames()
        {
            if (Frame == null) return;

            

            switch (CurrentAction)
            {
                case MirAction.Walking:
                case MirAction.Running:
                    //这个是干嘛啊
                    if (!GameScene.CanMove) return;

                    GameScene.Scene.MapControl.TextureValid = false;

                    if (SkipFrames) UpdateFrame();

                    if (UpdateFrame() >= Frame.Count)
                    {
                        FrameIndex = Frame.Count - 1;
                        SetAction();
                    }
                    else
                    {
                        switch (FrameIndex)
                        {
                            case 0:
                                PlayWalkSound(true);
                                break;
                            case 3:
                                PlayWalkSound(false);
                                break;
                        }
                    }
                    break;
                case MirAction.Pushed:
                    if (!GameScene.CanMove) return;

                    GameScene.Scene.MapControl.TextureValid = false;

                    FrameIndex -= 2;

                    if (FrameIndex < 0)
                    {
                        FrameIndex = 0;
                        SetAction();
                    }
                    break;
                case MirAction.Show:
                    if (CMain.Time >= NextMotion)
                    {
                        GameScene.Scene.MapControl.TextureValid = false;

                        if (SkipFrames) UpdateFrame();

                        if (UpdateFrame() >= Frame.Count)
                        {
                            switch (BaseImage)
                            {
                                case Monster.ZumaStatue:
                                case Monster.ZumaGuardian:
                                case Monster.RedThunderZuma:
                                case Monster.FrozenRedZuma:
                                case Monster.FrozenZumaStatue:
                                case Monster.FrozenZumaGuardian:
                                case Monster.ZumaTaurus:
                                    Stoned = false;
                                    break;
                                case Monster.Shinsu:
                                    BodyLibrary = Libraries.Monsters[(ushort)Monster.Shinsu1];
                                    BaseImage = Monster.Shinsu1;
                                    BaseSound = (ushort)BaseImage * 10;
                                    Frames = FrameSet.Monsters[2];
                                    break;
                            }

                            FrameIndex = Frame.Count - 1;
                            SetAction();
                        }
                        else
                        {
                            NextMotion += FrameInterval;
                        }
                    }
                    break;
                case MirAction.Hide:
                    if (CMain.Time >= NextMotion)
                    {
                        GameScene.Scene.MapControl.TextureValid = false;

                        if (SkipFrames) UpdateFrame();

                        if (UpdateFrame() >= Frame.Count)
                        {
                            switch (BaseImage)
                            {
                                //针对食人花之类的，隐藏就删除掉
                                case Monster.CannibalPlant:
                                case Monster.EvilCentipede:
                                case Monster.DigOutZombie:
                                case Monster.DeathCrawler:
                                case Monster.CreeperPlant:
                                case Monster.IcePhantom:
                                case Monster.Monster428:
                                case Monster.Monster438:
                                    Remove();
                                    return;
                                case Monster.ZumaStatue:
                                case Monster.ZumaGuardian:
                                case Monster.RedThunderZuma:
                                case Monster.FrozenRedZuma:
                                case Monster.FrozenZumaStatue:
                                case Monster.FrozenZumaGuardian:
                                case Monster.ZumaTaurus:
                                    Stoned = true;
                                    return;
                            }


                            FrameIndex = Frame.Count - 1;
                            SetAction();
                        }
                        else
                        {
                            NextMotion += FrameInterval;
                        }
                    }
                    break;
                case MirAction.Appear:
                case MirAction.Standing:
                case MirAction.Stoned:
                    if (CMain.Time >= NextMotion)
                    {
                        GameScene.Scene.MapControl.TextureValid = false;

                        if (SkipFrames) UpdateFrame();

                        if (UpdateFrame() >= Frame.Count)
                        {
                            if (CurrentAction == MirAction.Standing)
                            {
                                switch (BaseImage)
                                {
                                    case Monster.SnakeTotem://SummonSnakes Totem
                                        if (TrackableEffect.GetOwnerEffectID(this.ObjectID, "SnakeTotem") < 0)
                                            Effects.Add(new TrackableEffect(new Effect(Libraries.Monsters[(ushort)Monster.SnakeTotem], 2, 10, 1500, this) { Repeat = true }, "SnakeTotem"));
                                        break;
                                    case Monster.PalaceWall1:
                                        //Effects.Add(new Effect(Libraries.Effect, 196, 1, 1000, this) { DrawBehind = true, d });
                                        //Libraries.Effect.Draw(196, DrawLocation, Color.White, true);
                                        //Libraries.Effect.DrawBlend(196, DrawLocation, Color.White, true);
                                        break;
                                }
                            }

                            FrameIndex = Frame.Count - 1;
                            SetAction();
                        }
                        else
                        {
                            NextMotion += FrameInterval;
                        }
                    }
                    break;
                case MirAction.Attack1:
                    if (CMain.Time >= NextMotion)
                    {
                        GameScene.Scene.MapControl.TextureValid = false;

                        if (SkipFrames) UpdateFrame();

                        if (UpdateFrame() >= Frame.Count)
                        {
                            FrameIndex = Frame.Count - 1;
                            if (SetAction())
                            {
                                switch (BaseImage)
                                {
                                    case Monster.EvilCentipede:
                                        Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.EvilCentipede], 42, 10, 600, this));
                                        break;
                                    case Monster.ToxicGhoul:
                                        SoundManager.PlaySound(BaseSound + 4);
                                        Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ToxicGhoul], 224 + (int)Direction * 6, 6, 600, this));
                                        break;
                                }
                            }
                        }
                        else
                        {
                            switch (FrameIndex)
                            {
                                case 3:
                                    {
                                        PlaySwingSound();
                                        switch (BaseImage)
                                        {
                                            case Monster.RightGuard:
                                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.RightGuard], 272 + (int)Direction * 3, 3, 3 * Frame.Interval, this));
                                                break;
                                            case Monster.LeftGuard:
                                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.LeftGuard], 272 + (int)Direction * 3, 3, 3 * Frame.Interval, this));
                                                break;
                                            case Monster.Shinsu1:
                                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Shinsu1], 224 + (int)Direction * 6, 6, 6 * Frame.Interval, this));
                                                break;
                                        }
                                        break;
                                    }
                            }
                            NextMotion += FrameInterval;
                        }
                    }
                    break;
                case MirAction.SitDown:
                    if (CMain.Time >= NextMotion)
                    {
                        GameScene.Scene.MapControl.TextureValid = false;

                        if (SkipFrames) UpdateFrame();

                        if (UpdateFrame() >= Frame.Count)
                        {
                            FrameIndex = Frame.Count - 1;
                            SetAction();
                        }
                        else
                        {
                            NextMotion += FrameInterval;
                        }
                    }
                    break;
                case MirAction.Attack2:
                    if (CMain.Time >= NextMotion)
                    {
                        GameScene.Scene.MapControl.TextureValid = false;

                        if (SkipFrames) UpdateFrame();

                        if (UpdateFrame() >= Frame.Count)
                        {
                            FrameIndex = Frame.Count - 1;
                            SetAction();
                        }
                        else
                        {
                            switch (FrameIndex)
                            {
                                case 1:
                                    {
                                        switch (BaseImage)
                                        {
                                            case Monster.BabySnowMan:
                                                if (FrameIndex == 1)
                                                {
                                                    if (TrackableEffect.GetOwnerEffectID(this.ObjectID, "SnowmanSnow") < 0)
                                                        Effects.Add(new TrackableEffect(new Effect(Libraries.Pets[((ushort)BaseImage) - 10000], 208, 11, 1500, this), "SnowmanSnow"));
                                                }
                                                break;
                                        }
                                    }
                                    break;
                                case 3:
                                    {
                                        switch(BaseImage)
                                        {
                                            case Monster.Behemoth:
                                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Behemoth], 768, 10, Frame.Count * Frame.Interval, this));
                                                break;
                                            case Monster.FlameQueen:
                                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FlameQueen], 720, 9, Frame.Count * Frame.Interval, this));
                                                break;
                                        }
                                    }
                                    break;
  

                                case 10:
                                    {
                                        switch(BaseImage)
                                        {
                                            case Monster.StoningStatue:
                                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.StoningStatue], 642, 15, 15 * 100, this));
                                                break;
                                        }
                                    }
                                    break;
                                case 19:
                                    {
                                        switch(BaseImage)
                                        {
                                            case Monster.StoningStatue:
                                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.StoningStatue], 624, 8, 8 * 100, this));
                                                break;
                                        }
                                    }
                                    break;
                            }
                            if (FrameIndex == 3) PlaySwingSound();
                            NextMotion += FrameInterval;
                        }
                    }
                    break;
                case MirAction.Attack3:
                    if (CMain.Time >= NextMotion)
                    {
                        GameScene.Scene.MapControl.TextureValid = false;

                        if (SkipFrames) UpdateFrame();

                        if (UpdateFrame() >= Frame.Count)
                        {
                            FrameIndex = Frame.Count - 1;
                            SetAction();
                        }
                        else
                        {
                            switch (FrameIndex)
                            {
                                case 1:
                                    switch (BaseImage)
                                    {
                                        case Monster.OlympicFlame:
                                            if (TrackableEffect.GetOwnerEffectID(this.ObjectID, "CreatureFlame") < 0)
                                                Effects.Add(new TrackableEffect(new Effect(Libraries.Pets[((ushort)BaseImage) - 10000], 280, 4, 800, this), "CreatureFlame"));
                                            break;
                                    }
                                    break;
                                case 3:
                                    {
                                        switch (BaseImage)
                                        {
                                            case Monster.WingedTigerLord:
                                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.WingedTigerLord], 632, 8, 600, this, 0, true));
                                                break;
                                        }
                                    }
                                    break;
                                case 4:
                                    switch (BaseImage)
                                    {
                                        case Monster.OlympicFlame:
                                            if (TrackableEffect.GetOwnerEffectID(this.ObjectID, "CreatureSmoke") < 0)
                                                Effects.Add(new TrackableEffect(new Effect(Libraries.Pets[((ushort)BaseImage) - 10000], 256, 3, 1000, this), "CreatureSmoke"));
                                            break;
                                    }
                                    break;
                            }
                            NextMotion += FrameInterval;
                        }
                    }
                    break;
                case MirAction.Attack4:
                    if (CMain.Time >= NextMotion)
                    {
                        GameScene.Scene.MapControl.TextureValid = false;

                        if (SkipFrames) UpdateFrame();

                        if (UpdateFrame() >= Frame.Count)
                        {
                            FrameIndex = Frame.Count - 1;
                            SetAction();
                        }
                        else
                        {
                            NextMotion += FrameInterval;
                        }
                    }
                    break;
                case MirAction.Attack5:
                    if (CMain.Time >= NextMotion)
                    {
                        GameScene.Scene.MapControl.TextureValid = false;

                        if (SkipFrames) UpdateFrame();

                        if (UpdateFrame() >= Frame.Count)
                        {
                            FrameIndex = Frame.Count - 1;
                            SetAction();
                        }
                        else
                        {
                            NextMotion += FrameInterval;
                        }
                    }
                    break;
                case MirAction.AttackRange1:
                    if (CMain.Time >= NextMotion)
                    {
                        GameScene.Scene.MapControl.TextureValid = false;

                        if (SkipFrames) UpdateFrame();

                        if (UpdateFrame() >= Frame.Count)
                        {
                            switch (BaseImage)
                            {
                                case Monster.DragonStatue:
                                    MapObject ob = MapControl.GetObject(TargetID);
                                    if (ob != null)
                                    {
                                        ob.Effects.Add(new Effect(Libraries.Dragon, 350, 35, 1200, ob));
                                        SoundManager.PlaySound(BaseSound + 6);
                                    }
                                    break;
                            }
                            FrameIndex = Frame.Count - 1;
                            SetAction();
                        }
                        else
                        {
                            if (FrameIndex == 2) PlaySwingSound();
                            MapObject ob = null;
                            Missile missile;
                            switch (FrameIndex)
                            {
                                case 1:
                                    {
                                        switch (BaseImage)
                                        {
                                            case Monster.GuardianRock:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Magic2, 1410, 10, 400, ob));
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                }
                                                break;
                                     
                                        }
                                        break;
                                    }
                                case 2:
                                    {
                                        switch (BaseImage)
                                        {
                                            case Monster.LeftGuard:
                                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.LeftGuard], 336 + (int)Direction * 3, 3, 3 * Frame.Interval, this));
                                                break;
                                            case Monster.ManectricClaw:
                                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ManectricClaw], 304 + (int)Direction * 10, 10, 10 * Frame.Interval, this));
                                                break;
                                            case Monster.FlameSpear:
                                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FlameSpear], 544 + (int)Direction * 10, 10, 10 * 100, this));
                                                break;
                                            case Monster.DarkWraith://暗黑战士 丢冰球
                                                missile = CreateProjectile(796, Libraries.Monsters[(ushort)Monster.DarkWraith], true, 5, 20, 0,false);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        //440 + (int)Direction * 10
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.DarkWraith], 836, 6, 1000, missile.Target));
                                                    };
                                                }
                                                break;
                                        }
                                        break;
                                    }
                                case 4:
                                    {
                                        switch (BaseImage)
                                        {
                                            case Monster.AxeSkeleton:
                                                if (MapControl.GetObject(TargetID) != null)
                                                    CreateProjectile(224, Libraries.Monsters[(ushort)Monster.AxeSkeleton], false, 3, 30, 0);
                                                break;
                                            case Monster.RestlessJar:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {

                                                    MirDirection direction = Functions.DirectionFromPoint(CurrentLocation, ob.CurrentLocation);
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.RestlessJar], 391 + (int)direction * 10, 10, 700, ob));
                                                }
                                                break;
                                            case Monster.ChieftainSword://龙阳王，砸地板
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    MirDirection direction = Functions.DirectionFromPoint(CurrentLocation, ob.CurrentLocation);
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ChieftainSword], 1175, 12, 2400, ob));
                                                }
                                                break;
                                            case Monster.GeneralJinmYo://灵猫将军，雷电
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.GeneralJinmYo], 512, 10, 2000, ob));
                                                }
                                                break;
                                            case Monster.KillerPlant://黑暗船长，雷电
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.KillerPlant], 1200, 12, 2000, ob));
                                                }
                                                break;

                                                
                                            case Monster.BurningZombie://火焰僵尸，火球？
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    MirDirection direction = Functions.DirectionFromPoint(CurrentLocation, ob.CurrentLocation);
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.BurningZombie], 362, 10, 2000, ob));
                                                }
                                                break;
                                            case Monster.SwampWarrior://神殿树人，毒蘑菇？
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.SwampWarrior], 392, 9, 1800, ob));
                                                }
                                                break;


                                            case Monster.AssassinBird://神殿刺鸟，禁锢？
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.AssassinBird], 392, 8, 1600, ob));
                                                }
                                                break;
                                            case Monster.TreeQueen://树的女王，禁锢？
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.TreeQueen], 57, 9, 1800, ob));
                                                }
                                                break;
                                                
                                            case Monster.SeedingsGeneral://灵猫圣兽，拍地板
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    MirDirection direction = Functions.DirectionFromPoint(CurrentLocation, ob.CurrentLocation);
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.SeedingsGeneral], 1264, 9, 1000, ob));
                                                }
                                                break;
                                            case Monster.Jar2://青花圣壶 丢冰弹
                                                missile = CreateProjectile(410, Libraries.Magic2, true, 4, 30, 6);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        //440 + (int)Direction * 10
                                                       // missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Jar2], 624, 8, 1000, missile.Target) { Blend = false });
                                                    };
                                                }
                                                break;
                                            case Monster.Monster407://冰宫射手 射箭
                                                missile = CreateProjectile(344, Libraries.Monsters[(ushort)Monster.Monster407], true, 5, 20, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        //440 + (int)Direction * 10
                                                        //missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster407], 656, 6, 1000, missile.Target));
                                                    };
                                                }
                                                break;
                                         
                                                


                                            case Monster.FrozenMagician://冰魄法师 丢火球
                                                missile = CreateProjectile(560, Libraries.Monsters[(ushort)Monster.FrozenMagician], true, 6, 20, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        //440 + (int)Direction * 10
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FrozenMagician], 656, 6, 1000, missile.Target) );
                                                    };
                                                }
                                                break;
                                            case Monster.SnowYeti://冰魄雪人 丢火球
                                                missile = CreateProjectile(560, Libraries.Monsters[(ushort)Monster.SnowYeti], true, 6, 20, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        //440 + (int)Direction * 10
                                                        //missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FrozenMagician], 656, 6, 1000, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.Monster403://花仙子
                                                missile = CreateProjectile(331, Libraries.Monsters[(ushort)Monster.Monster403], true, 6, 20, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        //440 + (int)Direction * 10
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster403], 427, 9, 1000, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.Monster409://花魁
                                                missile = CreateProjectile(467, Libraries.Monsters[(ushort)Monster.Monster409], true, 5, 20, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        //440 + (int)Direction * 10
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster409], 580,6, 1000, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.Monster410://冰宫鼠卫
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster410], 384, 7, 700, ob));
                                                }
                                                break;
                                            case Monster.Monster411://冰宫骑士
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster411], 342, 6, 600, ob));
                                                }
                                                break;
                                            case Monster.Monster412://冰宫刀卫
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster412], 320, 6, 600, ob));
                                                }
                                                break;
                                            case Monster.Monster413://冰宫护法
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster413], 470, 15, 1500, ob));
                                                }
                                                break;
                                            case Monster.Monster416://冰宫画卷
                                                missile = CreateProjectile(300, Libraries.Monsters[(ushort)Monster.Monster416], true, 5, 20, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        //440 + (int)Direction * 10
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster416], 380, 8, 800, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.Monster417://冰宫画卷
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster417], 299, 8, 800, ob));
                                                }
                                                break;
                                            case Monster.Monster418://冰宫学者
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster418], 374, 13, 1300, ob));
                                                }
                                                break;
                                            case Monster.Monster419://冰宫巫师
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster419], 392, 7, 700, ob));
                                                }
                                                break;
                                            case Monster.Monster420://冰宫祭师
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster420], 463, 10, 1000, ob));
                                                }
                                                break;
                                       

                                            case Monster.DarkSpirit://幽灵战士 丢土球
                                                missile = CreateProjectile(512, Libraries.Monsters[(ushort)Monster.DarkSpirit], true, 6, 30, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        //440 + (int)Direction * 10
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.DarkSpirit], 608, 10, 1000, missile.Target));
                                                    };
                                                }
                                                break;

                                                
                                            case Monster.OmaMage://奥玛法师 丢冰弹
                                                missile = CreateProjectile(392, Libraries.Monsters[(ushort)Monster.OmaMage], true, 8, 20, 0);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.OmaMage], 520, 9, 900, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.FloatingWraith://幽灵射手 丢冰弹
                                                missile = CreateProjectile(248, Libraries.Monsters[(ushort)Monster.FloatingWraith], true, 2, 50, 0);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        //missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.OmaMage], 520, 9, 900, missile.Target));
                                                        SoundManager.PlaySound(BaseSound + 6);
                                                    };
                                                }
                                                break;
                                            case Monster.AvengingSpirit://复仇的恶灵 丢冰弹
                                                missile = CreateProjectile(368, Libraries.Monsters[(ushort)Monster.AvengingSpirit], true, 4, 40, 0);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.AvengingSpirit], 432, 10, 1000, missile.Target));
                                                        SoundManager.PlaySound(BaseSound + 6);
                                                    };
                                                }
                                                break;
                                            case Monster.AvengingWarrior://复仇的恶灵 丢冰弹
                                                missile = CreateProjectile(312, Libraries.Monsters[(ushort)Monster.AvengingWarrior], true, 5, 40, 0);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.AvengingWarrior], 392, 6, 1200, missile.Target));
                                                        SoundManager.PlaySound(BaseSound + 6);
                                                    };
                                                }
                                                break;

                                            case Monster.IcePhantom://复仇的恶灵 丢火蛇
                                                missile = CreateProjectile(779, Libraries.Monsters[(ushort)Monster.IcePhantom], true, 8, 40, 0,false);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.IcePhantom], 682, 10, 1000, missile.Target));
                                                        SoundManager.PlaySound(BaseSound + 6);
                                                    };
                                                }
                                                break;

                                            case Monster.ClawBeast://水手长，放雷电
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ClawBeast], 815, 10, 1500, new Point(ob.CurrentLocation.X, ob.CurrentLocation.Y)));
                                                }
                                                break;
                                            case Monster.CreeperPlant://攀缘花，放地钉
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.CreeperPlant], 256, 10, 1000, new Point(ob.CurrentLocation.X, ob.CurrentLocation.Y)));
                                                }
                                                break;
                                            case Monster.ShellFighter://斗争者，全屏毒？
                                                Point source = new Point(User.CurrentLocation.X , User.CurrentLocation.Y );

                                                MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ShellFighter], 776, 30, 3000, source));
                                                break;
                                            case Monster.OmaCannibal://奥玛食人族
                                                missile = CreateProjectile(360, Libraries.Monsters[(ushort)Monster.OmaCannibal], true, 6, 20, 0);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.OmaCannibal], 456, 7, 700, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.Monster440://昆仑叛军射手 射箭
                                                if (MapControl.GetObject(TargetID) != null)
                                                {
                                                    CreateProjectile(38, Libraries.Monsters[(ushort)Monster.ArcherGuard], false, 3, 30, 6);
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                }
                                     
                                                break;

                                            case Monster.Monster442://昆仑叛军射手 射箭
                                                if (MapControl.GetObject(TargetID) != null)
                                                {
                                                    CreateProjectile(400, Libraries.Monsters[(ushort)Monster.Monster442], true, 6, 30, 0);
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                }

                                                break;
                                            case Monster.Monster424://昆仑虎 猛扑
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster424], 478, 10, 1000, ob));
                                                }

                                                break;
                                            case Monster.Monster430://千面妖王，放冰
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster430], 432, 10, 1000, new Point(ob.CurrentLocation.X, ob.CurrentLocation.Y)));
                                                }
                                                break;
                                            case Monster.Monster438://盘蟹花 猛扑
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster438], 424, 5, 500, ob));
                                                }
                                                break;
                                            case Monster.Monster443://昆仑叛军射手 射箭
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster443], 440, 14, 1400, ob));
                                                }
                                                break;
                                            case Monster.Monster446://昆仑终极BOSS
                                                missile = CreateProjectile(896, Libraries.Monsters[(ushort)Monster.Monster446], true, 3, 20, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster446], 944, 8, 800, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.Monster450://毒妖射手 射箭
                                                missile = CreateProjectile(240, Libraries.Monsters[(ushort)Monster.Monster450], true, 3, 20, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster450], 288, 10, 1000, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.Monster451://毒妖射手 射箭
                                                missile = CreateProjectile(399, Libraries.Monsters[(ushort)Monster.Monster451], true, 4, 20, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster451], 479, 7, 700, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.Monster453:// Monster453 巨斧妖 毒妖林小BOSS
                                                missile = CreateProjectile(648, Libraries.Monsters[(ushort)Monster.Monster453], true, 5, 20, -5);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster453], 668, 6, 600, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.Monster454:// Monster453 毒妖女皇 毒妖林小BOSS
                                                missile = CreateProjectile(1136, Libraries.Monsters[(ushort)Monster.Monster454], true, 6, 20, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster454], 1232, 7, 700, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.Dark:
                                                if (MapControl.GetObject(TargetID) != null)
                                                    CreateProjectile(224, Libraries.Monsters[(ushort)Monster.Dark], false, 3, 30, 0);
                                                break;
                                            case Monster.ZumaArcher:
                                            case Monster.BoneArcher:
                                                if (MapControl.GetObject(TargetID) != null)
                                                {
                                                    CreateProjectile(224, Libraries.Monsters[(ushort)Monster.ZumaArcher], false, 1, 30, 0);
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                }
                                                break;
                                            case Monster.RedThunderZuma:
                                            case Monster.FrozenRedZuma:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Dragon, 400 + CMain.Random.Next(3) * 10, 5, 300, ob));
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                }
                                                break;
                                            case Monster.BoneLord:
                                                if (MapControl.GetObject(TargetID) != null)
                                                    CreateProjectile(784, Libraries.Monsters[(ushort)Monster.BoneLord], true, 6, 30, 0, false);
                                                break;
                                            case Monster.RightGuard:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Magic2, 10, 5, 300, ob));
                                                }
                                                break;
                                            case Monster.LeftGuard:
                                                if (MapControl.GetObject(TargetID) != null)
                                                {
                                                    CreateProjectile(10, Libraries.Magic, true, 6, 30, 4);
                                                }
                                                break;
                                            case Monster.MinotaurKing:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.MinotaurKing], 320, 20, 1000, ob));
                                                }
                                                break;
                                            case Monster.FrostTiger:
                                                if (MapControl.GetObject(TargetID) != null)
                                                {
                                                    CreateProjectile(410, Libraries.Magic2, true, 4, 30, 6);
                                                }
                                                break;
                                            case Monster.Yimoogi:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Magic2, 1250, 15, 1000, ob));
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                }
                                                break;
                                            case Monster.HolyDeva:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Magic2, 10, 5, 300, ob));
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                }
                                                break;
                                            case Monster.CrossbowOma:
                                                if (MapControl.GetObject(TargetID) != null)
                                                    CreateProjectile(38, Libraries.Monsters[(ushort)Monster.CrossbowOma], false, 1, 30, 6);
                                                break;
                                            case Monster.WingedOma:
                                                missile = CreateProjectile(224, Libraries.Monsters[(ushort)Monster.WingedOma], false, 6, 30, 0, false);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.WingedOma], 272, 2, 150, missile.Target) { Blend = false });
                                                    };
                                                }
                                                break;
                                            case Monster.PoisonHugger:
                                                missile = CreateProjectile(208, Libraries.Monsters[(ushort)Monster.PoisonHugger], true, 1, 30, 0);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.PoisonHugger], 224, 5, 150, missile.Target) { Blend = true });
                                                    };
                                                }
                                                break;
                                            case Monster.RedFoxman:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.RedFoxman], 224, 9, 300, ob));
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                }
                                                break;
                                            case Monster.CatShaman:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.CatShaman], 728, 12, 500, ob) { Blend = true });
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.CatShaman], 740, 14, 500, ob) { Blend = true });
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.CatShaman], 754, 7, 500, ob) { Blend = true });
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                }
                                                break;
                                            case Monster.Monster425:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster425], 552, 10, 1000, ob) { Blend = true });
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster425], 542, 10, 1000, ob) { Blend = true });
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                }
                                                break;
                                            case Monster.Monster426:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster426], 480, 15, 1500, ob) { Blend = true });
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                }
                                                break;
                                            case Monster.Monster427:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster427], 512, 9, 900, ob) { Blend = true });
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                }
                                                break;
                                            case Monster.Monster428:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster428], 384, 6, 600, ob) { Blend = true });
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster428], 394, 6, 600, ob) { Blend = true });
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                }
                                                break;
                                            case Monster.Monster429:
                                                missile = CreateProjectile(470, Libraries.Monsters[(ushort)Monster.Monster429], true, 4, 30, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster429], 534, 8, 800, missile.Target) { Blend = true });
                                                    };
                                                }
                                                break;
                                           

                                            case Monster.WhiteFoxman:
                                                missile = CreateProjectile(1160, Libraries.Magic, true, 3, 30, 7);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.WhiteFoxman], 352, 10, 600, missile.Target));
                                                        SoundManager.PlaySound(BaseSound + 6);
                                                    };
                                                }
                                                break;
                                            case Monster.AncientBringer:
                                                List<MapObject> listp =  MapControl.GetPlayers(CurrentLocation, 7);
                                                foreach(MapObject p in listp)
                                                {
                                                    int duration = Functions.MaxDistance(CurrentLocation, p.CurrentLocation) * 50;
                                                    Missile _missile = new Missile(Libraries.Monsters[(ushort)Monster.AncientBringer], 688, duration / 30, duration, this, p.CurrentLocation, false)
                                                    {
                                                        Target = p,
                                                        Interval = 30,
                                                        FrameCount = 4,
                                                        Blend = true,
                                                        Skip = 0
                                                    };
                                                    Effects.Add(_missile);
                                                }
                                                
                                                break;
                                            case Monster.TrapRock:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.TrapRock], 26, 10, 600, ob));
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                }
                                                break;
                                            
                                            case Monster.HedgeKekTal:
                                                if (MapControl.GetObject(TargetID) != null)
                                                    CreateProjectile(38, Libraries.Monsters[(ushort)Monster.HedgeKekTal], false, 4, 30, 6);
                                                break;
                                            case Monster.BigHedgeKekTal:
                                                if (MapControl.GetObject(TargetID) != null)
                                                    CreateProjectile(38, Libraries.Monsters[(ushort)Monster.BigHedgeKekTal], false, 4, 30, 6);
                                                break;
                                            case Monster.EvilMir:
                                                missile = CreateProjectile(60, Libraries.Dragon, true, 10, 10, 0);

                                                if (missile.Direction > 12)
                                                    missile.Direction = 12;
                                                if (missile.Direction < 7)
                                                    missile.Direction = 7;

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Dragon, 200, 20, 600, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.ArcherGuard:
                                                if (MapControl.GetObject(TargetID) != null)
                                                    CreateProjectile(38, Libraries.Monsters[(ushort)Monster.ArcherGuard], false, 3, 30, 6);
                                                break;
                                            case Monster.SpittingToad:
                                                if (MapControl.GetObject(TargetID) != null)
                                                    CreateProjectile(280, Libraries.Monsters[(ushort)Monster.SpittingToad], true, 6, 30, 0);
                                                break;
                                            case Monster.ArcherGuard2:
                                                if (MapControl.GetObject(TargetID) != null)
                                                    CreateProjectile(38, Libraries.Monsters[(ushort)Monster.ArcherGuard], false, 3, 30, 6);
                                                break;
                                            case Monster.FinialTurtle:
                                                missile = CreateProjectile(272, Libraries.Monsters[(ushort)Monster.FinialTurtle], true, 3, 30, 0, true);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FinialTurtle], 320, 10, 500, missile.Target) { Blend = true });
                                                        SoundManager.PlaySound(20000 + (ushort)Spell.FrostCrunch * 10 + 2);
                                                    };
                                                }
                                                break;
                                            case Monster.HellBolt:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.HellBolt], 315, 10, 600, ob));
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                }
                                                break;
                                            case Monster.WitchDoctor:
                                                if (MapControl.GetObject(TargetID) != null)
                                                {
                                                    missile = CreateProjectile(313, Libraries.Monsters[(ushort)Monster.WitchDoctor], true, 5, 30, -5, false);

                                                    if (missile.Target != null)
                                                    {
                                                        missile.Complete += (o, e) =>
                                                        {
                                                            if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                            missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.WitchDoctor], 318, 10, 600, missile.Target));
                                                            SoundManager.PlaySound(BaseSound + 6);
                                                        };
                                                    }
                                                }
                                                break;
                                            case Monster.WingedTigerLord:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.WingedTigerLord], 640, 10, 800, ob, CMain.Time + 400, false));
                                                }
                                                break;
                                            case Monster.TrollBomber:
                                                missile = CreateProjectile(208, Libraries.Monsters[(ushort)Monster.TrollBomber], false, 4, 40, -4, false);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.TrollBomber], 212, 6, 600, missile.Target) { Blend = true });
                                                    };
                                                }
                                                break;
                                            case Monster.TrollStoner:
                                                missile = CreateProjectile(208, Libraries.Monsters[(ushort)Monster.TrollStoner], false, 4, 40, -4, false);
                                                break;
                                            case Monster.FlameMage:
                                                missile = CreateProjectile(544, Libraries.Monsters[(ushort)Monster.FlameMage], true, 3, 20, 0);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FlameMage], 592, 10, 1000, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.FlameScythe:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FlameScythe], 586, 9, 900, ob));
                                                }
                                                break;
                                            case Monster.FlameAssassin:
                                                missile = CreateProjectile(592, Libraries.Monsters[(ushort)Monster.FlameAssassin], true, 3, 20, 0);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FlameAssassin], 640, 6, 600, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.FlameQueen:
                                                Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FlameQueen], 729, 10, Frame.Count * Frame.Interval, this));
                                                break;
                                        }
                                        break;
                                    }
                                case 5:
                                    
                                    break;
                                case 8:
                                    switch (BaseImage)
                                    {
                                        case Monster.FrozenArcher://雪原弓手
                                            missile = CreateProjectile(264, Libraries.Monsters[(ushort)Monster.FrozenArcher], true, 5, 20, 0);

                                            if (missile.Target != null)
                                            {
                                                missile.Complete += (o, e) =>
                                                {
                                                    SoundManager.PlaySound(BaseSound + 6);
                                                };
                                            }
                                            break;
                                    }
                                    break;
                            }
                            NextMotion += FrameInterval;
                        }
                    }
                    break;
                case MirAction.AttackRange2:
                    if (CMain.Time >= NextMotion)
                    {
                        GameScene.Scene.MapControl.TextureValid = false;

                        if (SkipFrames) UpdateFrame();

                        if (UpdateFrame() >= Frame.Count)
                        {
                            FrameIndex = Frame.Count - 1;
                            SetAction();
                        }
                        else
                        {
                            MapObject ob = null;
                            Missile missile;
                            switch (FrameIndex)
                            {
                                case 2:
                                    {
                                        switch (BaseImage)
                                        {
                                            case Monster.Monster412://冰宫刀卫
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster412], 374, 7, 700, ob));
                                                }
                                                break;

                                        }
                                        break;
                                    }
                                case 4:
                                    {
                                        switch (BaseImage)
                                        {
                                            case Monster.RedFoxman:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.RedFoxman], 233, 10, 400, ob));
                                                    SoundManager.PlaySound(BaseSound + 7);
                                                }
                                                break;
                                            case Monster.RestlessJar:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    MirDirection direction = Functions.DirectionFromPoint(CurrentLocation, ob.CurrentLocation);
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.RestlessJar], 391+ (int)direction *10, 10, 600, ob));
                                                }
                                                break;
                                            case Monster.ChieftainSword://龙阳王，砸地板
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    MirDirection direction = Functions.DirectionFromPoint(CurrentLocation, ob.CurrentLocation);
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ChieftainSword], 992, 10, 2000, ob));
                                                }
                                                break;
                                            case Monster.GeneralJinmYo://灵猫将军，雷电
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.GeneralJinmYo], 522, 7, 1000, ob));
                                                }
                                                break;
                                            case Monster.TreeQueen://树的女王，火雨？
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.TreeQueen], 44, 13, 1300, ob));
                                                }
                                                break;
                                            case Monster.Monster409://红花仙子，放冰钉钉
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster409], 547, 9, 1000, new Point(ob.CurrentLocation.X, ob.CurrentLocation.Y)));
                                                }
                                                break;
                                            case Monster.Monster407://冰宫射手 射箭
                                                missile = CreateProjectile(429, Libraries.Monsters[(ushort)Monster.Monster407], true, 5, 20, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        //440 + (int)Direction * 10
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster407], 424, 5, 800, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.Monster410://冰宫鼠卫
                                                missile = CreateProjectile(391, Libraries.Monsters[(ushort)Monster.Monster410], true, 1, 20, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster410], 407, 7, 700, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.Monster419://冰宫巫师
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster419], 438, 18, 1300, ob));
                                                }
                                                break;
                                            case Monster.Monster429:
                                                missile = CreateProjectile(470, Libraries.Monsters[(ushort)Monster.Monster429], true, 4, 30, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster429], 552, 8, 800, missile.Target) { Blend = true });
                                                    };
                                                }
                                                break;
                                            case Monster.Monster430://千面妖王，放火
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster430], 442, 10, 1000, ob));
                                                }
                                                break;
                                            case Monster.Monster438://盘蟹花 毒液攻击
                                                missile = CreateProjectile(429, Libraries.Monsters[(ushort)Monster.Monster438], true, 6, 30, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster438], 525, 7, 700, missile.Target) { Blend = true });
                                                    };
                                                }
                                                break;
                                            case Monster.Monster440://昆仑叛军射手 射箭
                                                ob = MapControl.GetObject(TargetID);
                                                missile = CreateProjectile(388, Libraries.Monsters[(ushort)Monster.Monster440], true, 5, 30, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        //440 + (int)Direction * 10
                                                        MirDirection direction = Functions.DirectionFromPoint(CurrentLocation, ob.CurrentLocation);
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster440], 332 + (int)direction * 7, 7, 700, missile.Target));
                                                    };
                                                }
                                                break;


                                            case Monster.Monster442://昆仑叛军射手 射箭
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster442], 512, 5, 500, ob));
                                                }

                                                break;
                                            case Monster.Monster446://昆仑终极BOSS
                                                missile = CreateProjectile(1176, Libraries.Monsters[(ushort)Monster.Monster446], true, 6, 20, -6);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster446], 1182, 8, 800, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.Monster451://Monster451 碑石妖 毒妖林小BOSS
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster451], 486, 12, 1200, ob));
                                                }
                                                break;
                                            case Monster.ShellFighter:
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ShellFighter], 744, 11, 1100, ob));
                                                }
                                                break;
                                            case Monster.WhiteFoxman:
                                                missile = CreateProjectile(1160, Libraries.Magic, true, 3, 30, 7);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.WhiteFoxman], 362, 20, 2400, missile.Target));
                                                        SoundManager.PlaySound(BaseSound + 7);
                                                    };
                                                }
                                                break;
                                            case Monster.SeedingsGeneral://灵猫圣兽
                                                missile = CreateProjectile(1273, Libraries.Monsters[(ushort)Monster.SeedingsGeneral], true, 4, 30, 0);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.SeedingsGeneral], 1337, 8, 1000, missile.Target));
                                                        SoundManager.PlaySound(BaseSound + 7);
                                                    };
                                                }
                                                break;
                                            case Monster.IcePhantom://复仇的恶灵 丢冰蛇
                                                missile = CreateProjectile(779, Libraries.Monsters[(ushort)Monster.IcePhantom], true, 8, 40, 0, false);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.IcePhantom], 772, 7, 1200, missile.Target));
                                                        SoundManager.PlaySound(BaseSound + 6);
                                                    };
                                                }
                                                break;
                                            case Monster.FrozenMagician://冰魄法师 丢火球
                                                missile = CreateProjectile(734, Libraries.Monsters[(ushort)Monster.FrozenMagician], true, 6, 20, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        //440 + (int)Direction * 10
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.FrozenMagician], 830, 10, 1500, missile.Target));
                                                    };
                                                }
                                                break;
                                            case Monster.TrollKing:
                                                missile = CreateProjectile(294, Libraries.Monsters[(ushort)Monster.TrollKing], false, 4, 40, -4, false);

                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.TrollKing], 298, 6, 600, missile.Target) { Blend = true });
                                                    };
                                                }
                                                break;
                                        }
                                        break;
                                    }
                            }
                            NextMotion += FrameInterval;
                        }
                    }
                    break;
                case MirAction.AttackRange3:
                    if (CMain.Time >= NextMotion)
                    {
                        GameScene.Scene.MapControl.TextureValid = false;

                        if (SkipFrames) UpdateFrame();

                        if (UpdateFrame() >= Frame.Count)
                        {
                            FrameIndex = Frame.Count - 1;
                            SetAction();
                        }
                        else
                        {
                            switch (FrameIndex)
                            {
                                case 4:
                                    {
                                        MapObject ob = null;
                                        Missile missile;
                                        switch (BaseImage)
                                        {
                                            case Monster.ChieftainSword://龙阳王，砸地板
                                                ob = MapControl.GetObject(TargetID);
                                                if (ob != null)
                                                {
                                                    MirDirection direction = Functions.DirectionFromPoint(CurrentLocation, ob.CurrentLocation);
                                                    ob.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.ChieftainSword], 1066, 10, 2000, ob));
                                                }
                                                break;
                                            case Monster.Monster429://昆仑道士 5种攻击手段
                                                missile = CreateProjectile(470, Libraries.Monsters[(ushort)Monster.Monster429], true, 4, 30, 0);
                                                if (missile.Target != null)
                                                {
                                                    missile.Complete += (o, e) =>
                                                    {
                                                        if (missile.Target.CurrentAction == MirAction.Dead) return;
                                                        missile.Target.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Monster429], 560, 8, 800, missile.Target) { Blend = true });
                                                    };
                                                }
                                                break;
                                        }
                                        break;
                                    }
                            }
                            NextMotion += FrameInterval;
                        }
                    }
                    break;
                case MirAction.Struck:
                    if (CMain.Time >= NextMotion)
                    {
                        GameScene.Scene.MapControl.TextureValid = false;

                        if (SkipFrames) UpdateFrame();

                        if (UpdateFrame() >= Frame.Count)
                        {
                            FrameIndex = Frame.Count - 1;
                            SetAction();
                        }
                        else
                        {
                            NextMotion += FrameInterval;
                        }
                    }
                    break;

                case MirAction.Die:
                    if (CMain.Time >= NextMotion)
                    {
                        GameScene.Scene.MapControl.TextureValid = false;

                        if (SkipFrames) UpdateFrame();

                        if (UpdateFrame() >= Frame.Count)
                        {
                            FrameIndex = Frame.Count - 1;
                            ActionFeed.Clear();
                            ActionFeed.Add(new QueuedAction { Action = MirAction.Dead, Direction = Direction, Location = CurrentLocation });
                            SetAction();
                        }
                        else
                        {
                            switch (FrameIndex)
                            {
                                case 1:
                                    switch (BaseImage)
                                    {
                                        case Monster.PoisonHugger:
                                            Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.PoisonHugger], 224, 5, Frame.Count * FrameInterval, this));
                                            break;
                                        case Monster.Hugger:
                                            Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.Hugger], 256, 8, Frame.Count * FrameInterval, this));
                                            break;
                                        case Monster.MutatedHugger:
                                            Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.MutatedHugger], 128, 7, Frame.Count * FrameInterval, this));
                                            break;
                                        case Monster.CyanoGhast:
                                            Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.CyanoGhast], 457, 7, Frame.Count * FrameInterval, this));
                                            break;
                                    }
                                    break;
                                case 3:
                                    PlayDeadSound();
                                    switch (BaseImage)
                                    {
                                        case Monster.BoneSpearman:
                                        case Monster.BoneBlademan:
                                        case Monster.BoneArcher:
                                            Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.BoneSpearman], 224, 8, Frame.Count * FrameInterval, this));
                                            break;
                                    }
                                    break;
                            }

                            NextMotion += FrameInterval;
                        }
                    }
                    break;
                case MirAction.Revive:
                    if (CMain.Time >= NextMotion)
                    {
                        GameScene.Scene.MapControl.TextureValid = false;

                        if (SkipFrames) UpdateFrame();

                        if (UpdateFrame() >= Frame.Count)
                        {
                            FrameIndex = Frame.Count - 1;
                            ActionFeed.Clear();
                            ActionFeed.Add(new QueuedAction { Action = MirAction.Standing, Direction = Direction, Location = CurrentLocation });
                            SetAction();
                        }
                        else
                        {
                            if (FrameIndex == 3)
                                PlayReviveSound();
                            NextMotion += FrameInterval;
                        }
                    }
                    break;
                case MirAction.Dead:
                    break;

            }

            if ((CurrentAction == MirAction.Standing || CurrentAction == MirAction.SitDown) && NextAction != null)
                SetAction();
            else if (CurrentAction == MirAction.Dead && NextAction != null && (NextAction.Action == MirAction.Skeleton || NextAction.Action == MirAction.Revive))
                SetAction();
        }
        public int UpdateFrame()
        {
            if (Frame == null) return 0;

            if (Frame.Reverse) return Math.Abs(--FrameIndex);

            return ++FrameIndex;
        }


        private Missile CreateProjectile(int baseIndex, MLibrary library, bool blend, int count, int interval, int skip, bool direction16 = true)
        {
            MapObject ob = MapControl.GetObject(TargetID);
            
            if (ob != null) TargetPoint = ob.CurrentLocation;

            int duration = Functions.MaxDistance(CurrentLocation, TargetPoint) * 50;


            Missile missile = new Missile(library, baseIndex, duration / interval, duration, this, TargetPoint, direction16)
            {
                Target = ob,
                Interval = interval,
                FrameCount = count,
                Blend = blend,
                Skip = skip
            };

            Effects.Add(missile);

            return missile;
        }

        private void PlaySummonSound()
        {
            switch (BaseImage)
            {
                case Monster.HellKnight1:
                case Monster.HellKnight2:
                case Monster.HellKnight3:
                case Monster.HellKnight4:
                    SoundManager.PlaySound(BaseSound + 0);
                    return;
                case Monster.BoneFamiliar:
                case Monster.Shinsu:
                case Monster.HolyDeva:
                    SoundManager.PlaySound(BaseSound + 5);
                    return;
            }
        }
        private void PlayWalkSound(bool left = true)
        {
            if (left)
            {
                switch (BaseImage)
                {
                    case Monster.WingedTigerLord:
                    case Monster.PoisonHugger:
                        SoundManager.PlaySound(BaseSound + 8);
                        return;
                }
            }
            else
            {
                switch (BaseImage)
                {
                    case Monster.WingedTigerLord:
                        SoundManager.PlaySound(BaseSound + 8);
                        return;
                    case Monster.PoisonHugger:
                        SoundManager.PlaySound(BaseSound + 9);
                        return;
                }
            }
        }
        public void PlayAppearSound()
        {
            switch (BaseImage)
            {
                case Monster.CannibalPlant:
                case Monster.EvilCentipede:
                    return;
                case Monster.ZumaArcher:
                case Monster.ZumaStatue:
                case Monster.ZumaGuardian:
                case Monster.RedThunderZuma:
                case Monster.FrozenRedZuma:
                case Monster.FrozenZumaStatue:
                case Monster.FrozenZumaGuardian:
                case Monster.ZumaTaurus:
                    if (Stoned) return;
                    break;
            }

            SoundManager.PlaySound(BaseSound);
        }
        public void PlayPopupSound()
        {
            switch (BaseImage)
            {
                case Monster.ZumaTaurus:
                case Monster.DigOutZombie:
                    SoundManager.PlaySound(BaseSound + 5);
                    return;
                case Monster.Shinsu:
                    SoundManager.PlaySound(BaseSound + 6);
                    return;
            }
            SoundManager.PlaySound(BaseSound);
        }
        public void PlayFlinchSound()
        {
            SoundManager.PlaySound(BaseSound + 2);
        }
        public void PlayStruckSound()
        {
            switch (StruckWeapon)
            {
                case 0:
                case 23:
                case 28:
                case 40:
                    SoundManager.PlaySound(SoundList.StruckWooden);
                    break;
                case 1:
                case 12:
                    SoundManager.PlaySound(SoundList.StruckShort);
                    break;
                case 2:
                case 8:
                case 11:
                case 15:
                case 18:
                case 20:
                case 25:
                case 31:
                case 33:
                case 34:
                case 37:
                case 41:
                    SoundManager.PlaySound(SoundList.StruckSword);
                    break;
                case 3:
                case 5:
                case 7:
                case 9:
                case 13:
                case 19:
                case 24:
                case 26:
                case 29:
                case 32:
                case 35:
                    SoundManager.PlaySound(SoundList.StruckSword2);
                    break;
                case 4:
                case 14:
                case 16:
                case 38:
                    SoundManager.PlaySound(SoundList.StruckAxe);
                    break;
                case 6:
                case 10:
                case 17:
                case 22:
                case 27:
                case 30:
                case 36:
                case 39:
                    SoundManager.PlaySound(SoundList.StruckShort);
                    break;
                case 21:
                    SoundManager.PlaySound(SoundList.StruckClub);
                    break;
            }
        }
        public void PlayAttackSound()
        {
            SoundManager.PlaySound(BaseSound + 1);
        }

        public void PlaySecondAttackSound()
        {
            SoundManager.PlaySound(BaseSound + 6);
        }
        public void PlayThirdAttackSound()
        {
            SoundManager.PlaySound(BaseSound + 7);
        }

        public void PlayFourthAttackSound()
        {
            SoundManager.PlaySound(BaseSound + 8);
        }
        public void PlaySwingSound()
        {
            SoundManager.PlaySound(BaseSound + 4);
        }
        public void PlayDieSound()
        {
            SoundManager.PlaySound(BaseSound + 3);
        }
        public void PlayDeadSound()
        {
            switch (BaseImage)
            {
                case Monster.CaveBat:
                case Monster.HellKnight1:
                case Monster.HellKnight2:
                case Monster.HellKnight3:
                case Monster.HellKnight4:
                case Monster.CyanoGhast:
                    SoundManager.PlaySound(BaseSound + 5);
                    return;
            }
        }
        public void PlayReviveSound()
        {
            switch (BaseImage)
            {
                case Monster.ClZombie:
                case Monster.NdZombie:
                case Monster.CrawlerZombie:
                    SoundManager.PlaySound(SoundList.ZombieRevive);
                    return;
            }
        }
        public void PlayRangeSound()
        {
            switch (BaseImage)
            {
                case Monster.TurtleKing:
                    return;
                case Monster.RedThunderZuma:
                case Monster.FrozenRedZuma:
                case Monster.KingScorpion:
                case Monster.DarkDevil:
                case Monster.Khazard:
                case Monster.BoneLord:
                case Monster.LeftGuard:
                case Monster.RightGuard:
                case Monster.FrostTiger:
                case Monster.GreatFoxSpirit:
                case Monster.BoneSpearman:
                case Monster.MinotaurKing:
                case Monster.WingedTigerLord:
                case Monster.ManectricClaw:
                case Monster.ManectricKing:
                case Monster.HellBolt:
                case Monster.WitchDoctor:
                case Monster.FlameSpear:
                case Monster.FlameMage:
                case Monster.FlameScythe:
                case Monster.FlameAssassin:
                case Monster.FlameQueen:
                    SoundManager.PlaySound(BaseSound + 5);
                    return;
                default:
                    PlayAttackSound();
                    return;
            }
        }
        public void PlaySecondRangeSound()
        {
            switch(BaseImage)
            {
                case Monster.TurtleKing:
                    return;
                default:
                    PlaySecondAttackSound();
                    return;
            }
        }

        public void PlayThirdRangeSound()
        {
            switch (BaseImage)
            {
                default:
                    PlayThirdAttackSound();
                    return;
            }
        }

        public void PlayPickupSound()
        {
            SoundManager.PlaySound(SoundList.PetPickup);
        }
        public void PlayPetSound()
        {
            int petSound = (ushort)BaseImage - 10000 + 10500;

            switch (BaseImage)
            {
                case Monster.Chick:
                case Monster.BabyPig:
                case Monster.Kitten:
                case Monster.BabySkeleton:
                case Monster.Baekdon:
                case Monster.Wimaen:
                case Monster.BlackKitten:
                case Monster.BabyDragon:
                case Monster.OlympicFlame:
                case Monster.BabySnowMan:
                case Monster.Frog:
                case Monster.BabyMonkey:
                case Monster.AngryBird:
                case Monster.Foxey:
                    SoundManager.PlaySound(petSound);
                    break;
            }
        }
        public override void Draw()
        {
            DrawBehindEffects(Settings.Effect);

            float oldOpacity = DXManager.Opacity;
            if (Hidden && !DXManager.Blending) DXManager.SetOpacity(0.5F);

            if (BodyLibrary == null || Frame == null) return;

            if (!DXManager.Blending && Frame.Blend)
                BodyLibrary.DrawBlend(DrawFrame, DrawLocation, DrawColour, true);
            else
                BodyLibrary.Draw(DrawFrame, DrawLocation, DrawColour, true);

            DXManager.SetOpacity(oldOpacity);
        }


        public override bool MouseOver(Point p)
        {
            return MapControl.MapLocation == CurrentLocation || BodyLibrary != null && BodyLibrary.VisiblePixel(DrawFrame, p.Subtract(FinalDrawLocation), false);
        }

        public override void DrawBehindEffects(bool effectsEnabled)
        {
            for (int i = 0; i < Effects.Count; i++)
            {
                if (!Effects[i].DrawBehind) continue;
                Effects[i].Draw();
            }
        }

        public override void DrawEffects(bool effectsEnabled)
        {
            if (!effectsEnabled) return;

            for (int i = 0; i < Effects.Count; i++)
            {
                if (Effects[i].DrawBehind) continue;
                Effects[i].Draw();
            }
 
            switch (BaseImage)
            {
                case Monster.Scarecrow:
                    switch (CurrentAction)
                    {
                        case MirAction.Die:
                            Libraries.Monsters[(ushort)Monster.Scarecrow].DrawBlend(224 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.CaveMaggot:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack1:
                            if (FrameIndex >= 1)
                                Libraries.Monsters[(ushort)Monster.CaveMaggot].DrawBlend(175 + FrameIndex + (int)Direction * 5, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.Skeleton:
                case Monster.BoneFighter:
                case Monster.AxeSkeleton:
                case Monster.BoneWarrior:
                case Monster.BoneElite:
                case Monster.BoneWhoo:
                    switch (CurrentAction)
                    {
                        case MirAction.Die:
                            Libraries.Monsters[(ushort)Monster.Skeleton].DrawBlend(224 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.WoomaTaurus:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.WoomaTaurus].DrawBlend(224 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.Dung:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack1:
                            if (FrameIndex >= 1)
                                Libraries.Monsters[(ushort)Monster.Dung].DrawBlend(223 + FrameIndex + (int)Direction * 5, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.WedgeMoth:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.WedgeMoth].DrawBlend(224 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.RedThunderZuma:
                case Monster.FrozenRedZuma:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)Monster.RedThunderZuma].DrawBlend(320 + FrameIndex + (int)Direction * 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Walking:
                        case MirAction.Pushed:
                            Libraries.Monsters[(ushort)Monster.RedThunderZuma].DrawBlend(352 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.RedThunderZuma].DrawBlend(400 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)Monster.RedThunderZuma].DrawBlend(448 + FrameIndex + (int)Direction * 2, DrawLocation, Color.White, true);
                            break;
                        case MirAction.AttackRange1:
                            Libraries.Monsters[(ushort)Monster.RedThunderZuma].DrawBlend(464 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.KingHog:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.KingHog].DrawBlend(224 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.DarkDevil:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.DarkDevil].DrawBlend(342 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.BoneLord:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)Monster.BoneLord].DrawBlend(400 + FrameIndex + (int)Direction * 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Walking:
                        case MirAction.Pushed:
                            Libraries.Monsters[(ushort)Monster.BoneLord].DrawBlend(432 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.BoneLord].DrawBlend(480 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack2:
                            Libraries.Monsters[(ushort)Monster.BoneLord].DrawBlend(528 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.AttackRange1:
                            Libraries.Monsters[(ushort)Monster.BoneLord].DrawBlend(576 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)Monster.BoneLord].DrawBlend(624 + FrameIndex + (int)Direction * 2, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                            Libraries.Monsters[(ushort)Monster.BoneLord].DrawBlend(640 + FrameIndex + (int)Direction * 20, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.HolyDeva:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)Monster.HolyDeva].Draw(226 + FrameIndex + (int)Direction * 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Walking:
                        case MirAction.Pushed:
                            Libraries.Monsters[(ushort)Monster.HolyDeva].Draw(258 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.AttackRange1:
                            Libraries.Monsters[(ushort)Monster.HolyDeva].Draw(306 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)Monster.HolyDeva].Draw(354 + FrameIndex + (int)Direction * 2, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                            if (FrameIndex <= 6) Libraries.Monsters[(ushort)Monster.HolyDeva].Draw(370 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Appear:
                            if (FrameIndex >= 5) Libraries.Monsters[(ushort)Monster.HolyDeva].Draw(418 + FrameIndex - 5, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.YinDevilNode:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)Monster.YinDevilNode].DrawBlend(22 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.YangDevilNode:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)Monster.YangDevilNode].DrawBlend(22 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.OmaKing:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack1:
                            if (FrameIndex >= 3) Libraries.Monsters[(ushort)Monster.OmaKing].DrawBlend((624 + FrameIndex + (int)Direction * 4) - 3, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack2:
                            Libraries.Monsters[(ushort)Monster.OmaKing].DrawBlend(656 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                            Libraries.Monsters[(ushort)Monster.OmaKing].DrawBlend(304 + FrameIndex + (int)Direction * 20, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.BlackFoxman:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack2:
                            if (FrameIndex >= 3) Libraries.Monsters[(ushort)Monster.BlackFoxman].DrawBlend((234 + FrameIndex + (int)Direction * 4) - 3, DrawLocation, Color.White, true);
                            break;
                    }
                    break;

                case Monster.ManectricKing:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)Monster.ManectricKing].DrawBlend(360 + FrameIndex + (int)Direction * 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Walking:
                            Libraries.Monsters[(ushort)Monster.ManectricKing].DrawBlend(392 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.ManectricKing].DrawBlend(440 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack2:
                            Libraries.Monsters[(ushort)Monster.ManectricKing].DrawBlend(576 + FrameIndex + (int)Direction * 8, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)Monster.ManectricKing].DrawBlend(488 + FrameIndex + (int)Direction * 2, DrawLocation, Color.White, true);
                            break;
                    }
                    break;            
                case Monster.ManectricStaff:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack2:
                            Libraries.Monsters[(ushort)Monster.ManectricStaff].DrawBlend(296 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.ManectricBlest:
                    switch(CurrentAction)
                    {
                        case MirAction.Attack2:
                            if (FrameIndex >= 4) Libraries.Monsters[(ushort)Monster.ManectricBlest].DrawBlend((328 + FrameIndex + (int)Direction * 4) - 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack3:
                            if (FrameIndex >= 2) Libraries.Monsters[(ushort)Monster.ManectricBlest].DrawBlend((360 + FrameIndex + (int)Direction * 5) - 2, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.Bear:
                    switch (CurrentAction)
                    {
                        case MirAction.AttackRange1:
                            Libraries.Monsters[(ushort)Monster.Bear].DrawBlend(312 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.WoodBox:
                    switch (CurrentAction)
                    {
                        case MirAction.Die:
                            Libraries.Monsters[(ushort)Monster.WoodBox].DrawBlend(103 + FrameIndex + (int)Direction * 20, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.KingGuard:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)Monster.KingGuard].DrawBlend(392 + FrameIndex + (int)Direction * 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Walking:
                            Libraries.Monsters[(ushort)Monster.KingGuard].DrawBlend(424 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.KingGuard].DrawBlend(472 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack2:
                            Libraries.Monsters[(ushort)Monster.KingGuard].DrawBlend(616 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Dead:
                            Libraries.Monsters[(ushort)Monster.KingGuard].DrawBlend(545 + FrameIndex + (int)Direction * 10, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Pushed:
                            Libraries.Monsters[(ushort)Monster.KingGuard].DrawBlend(352 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)Monster.KingGuard].DrawBlend(520 + FrameIndex + (int)Direction * 2, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                            Libraries.Monsters[(ushort)Monster.KingGuard].DrawBlend(534 + FrameIndex + (int)Direction * 10, DrawLocation, Color.White, true);
                            break;
                        case MirAction.AttackRange1:
                            Libraries.Monsters[(ushort)Monster.KingGuard].DrawBlend(664 + FrameIndex + (int)Direction * 8, DrawLocation, Color.White, true);
                            break;
                        case MirAction.AttackRange2:
                            Libraries.Monsters[(ushort)Monster.KingGuard].DrawBlend(728 + FrameIndex + (int)Direction * 7, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.SeedingsGeneral:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)Monster.SeedingsGeneral].DrawBlend(536 + FrameIndex + (int)Direction * 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Walking:
                            Libraries.Monsters[(ushort)Monster.SeedingsGeneral].DrawBlend(568 + FrameIndex + (int)Direction * 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.SeedingsGeneral].DrawBlend(704 + FrameIndex + (int)Direction * 9, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack2:
                            Libraries.Monsters[(ushort)Monster.SeedingsGeneral].DrawBlend(776 + FrameIndex + (int)Direction * 9, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Dead:
                            Libraries.Monsters[(ushort)Monster.SeedingsGeneral].DrawBlend(1015 + FrameIndex + (int)Direction * 1, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)Monster.SeedingsGeneral].DrawBlend(984 + FrameIndex + (int)Direction * 3, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                            Libraries.Monsters[(ushort)Monster.SeedingsGeneral].DrawBlend(1008 + FrameIndex + (int)Direction * 8, DrawLocation, Color.White, true);
                            break;
                        case MirAction.AttackRange1:
                            Libraries.Monsters[(ushort)Monster.SeedingsGeneral].DrawBlend(848 + FrameIndex + (int)Direction * 8, DrawLocation, Color.White, true);
                            break;
                        case MirAction.AttackRange2:
                            Libraries.Monsters[(ushort)Monster.SeedingsGeneral].DrawBlend(912 + FrameIndex + (int)Direction * 9, DrawLocation, Color.White, true);
                            break;
                    }
                    break;


                case Monster.HellSlasher:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack2:
                            if (FrameIndex >= 4 && FrameIndex < 8) Libraries.Monsters[(ushort)Monster.HellSlasher].DrawBlend((304 + FrameIndex + (int)Direction * 4) - 4, DrawLocation, Color.White, true);
                            break;
                    }
                    break;

                case Monster.HellPirate:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack2:
                            if (FrameIndex >= 3) Libraries.Monsters[(ushort)Monster.HellPirate].DrawBlend((280 + FrameIndex + (int)Direction * 4) - 3, DrawLocation, Color.White, true);
                            break;
                    }
                    break;

                case Monster.HellCannibal:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.HellCannibal].DrawBlend(304 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                    }
                    break;

                case Monster.HellKeeper:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack2:
                            Libraries.Monsters[(ushort)Monster.HellKeeper].DrawBlend(40 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.Manticore:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack2:
                            if (FrameIndex >= 3) Libraries.Monsters[(ushort)Monster.Manticore].DrawBlend((536 + FrameIndex + (int)Direction * 4) - 3, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.GuardianRock:
                    switch (CurrentAction)
                    {
                        case MirAction.AttackRange1:
                            Libraries.Monsters[(ushort)Monster.GuardianRock].DrawBlend(8 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.ThunderElement:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)Monster.ThunderElement].DrawBlend(44 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Walking:
                        case MirAction.Pushed:
                            Libraries.Monsters[(ushort)Monster.ThunderElement].DrawBlend(54 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.ThunderElement].DrawBlend(64 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)Monster.ThunderElement].DrawBlend(74 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                            Libraries.Monsters[(ushort)Monster.ThunderElement].DrawBlend(78 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.CloudElement:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)Monster.CloudElement].DrawBlend(44 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Walking:
                        case MirAction.Pushed:
                            Libraries.Monsters[(ushort)Monster.CloudElement].DrawBlend(54 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.CloudElement].DrawBlend(64 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)Monster.CloudElement].DrawBlend(74 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                            Libraries.Monsters[(ushort)Monster.CloudElement].DrawBlend(78 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.GreatFoxSpirit:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)Monster.GreatFoxSpirit].DrawBlend(Frame.Start + 30 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack1:
                        case MirAction.AttackRange1:
                            Libraries.Monsters[(ushort)Monster.GreatFoxSpirit].DrawBlend(Frame.Start + 30 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)Monster.GreatFoxSpirit].DrawBlend(Frame.Start + 30 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                            Libraries.Monsters[(ushort)Monster.GreatFoxSpirit].DrawBlend(318 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.TaoistGuard:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.TaoistGuard].DrawBlend(80 + ((int)Direction * 3) + FrameIndex, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.CyanoGhast: //mob glow effect
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)Monster.CyanoGhast].DrawBlend(448 + FrameIndex + (int)Direction * 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Walking:
                            Libraries.Monsters[(ushort)Monster.CyanoGhast].DrawBlend(480 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.CyanoGhast].DrawBlend(528 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)Monster.CyanoGhast].DrawBlend(576 + FrameIndex + (int)Direction * 2, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                        case MirAction.Revive:
                            Libraries.Monsters[(ushort)Monster.CyanoGhast].DrawBlend(592 + FrameIndex + (int)Direction * 10, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.MutatedManworm:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.MutatedManworm].DrawBlend(285 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack2:
                            Libraries.Monsters[(ushort)Monster.MutatedManworm].DrawBlend(333 + FrameIndex + (int)Direction * 8, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.CrazyManworm:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack2:
                            Libraries.Monsters[(ushort)Monster.CrazyManworm].DrawBlend(272 + FrameIndex + (int)Direction * 8, DrawLocation, Color.White, true);
                            break;
                    }
                    break;

                case Monster.Behemoth:
                    switch (CurrentAction)
                    {
                        case MirAction.Walking:
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)Monster.Behemoth].DrawBlend(464 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Standing:
                        case MirAction.Revive:
                            Libraries.Monsters[(ushort)Monster.Behemoth].DrawBlend(512 + FrameIndex + (int)Direction * 10, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack2:
                        case MirAction.Attack3:
                        case MirAction.AttackRange1:
                        case MirAction.AttackRange2:
                            Libraries.Monsters[(ushort)Monster.Behemoth].DrawBlend(592 + FrameIndex + (int)Direction * 7, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack1:
                            if (FrameIndex >= 4) Libraries.Monsters[(ushort)Monster.Behemoth].DrawBlend((667 + FrameIndex + (int)Direction * 2) - 4, DrawLocation, Color.White, true);
                            Libraries.Monsters[(ushort)Monster.Behemoth].DrawBlend(592 + FrameIndex + (int)Direction * 7, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                            if (FrameIndex >= 1) Libraries.Monsters[(ushort)Monster.Behemoth].DrawBlend(658 + FrameIndex - 1, DrawLocation, Color.White, true);
                            break;
                    }

                    if(CurrentAction != MirAction.Dead)
                        Libraries.Monsters[(ushort)Monster.Behemoth].DrawBlend(648 + FrameIndex, DrawLocation, Color.White, true);
                    break;

                case Monster.DarkDevourer:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)Monster.DarkDevourer].DrawBlend(272 + FrameIndex + (int)Direction * 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Walking:
                            Libraries.Monsters[(ushort)Monster.DarkDevourer].DrawBlend(304 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.DarkDevourer].DrawBlend(352 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.AttackRange1:
                            Libraries.Monsters[(ushort)Monster.DarkDevourer].DrawBlend(540 + FrameIndex + (int)Direction * 8, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)Monster.DarkDevourer].DrawBlend(400 + FrameIndex + (int)Direction * 2, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                        case MirAction.Revive:
                            Libraries.Monsters[(ushort)Monster.DarkDevourer].DrawBlend(416 + FrameIndex + (int)Direction * 8, DrawLocation, Color.White, true);
                            break;
                    }
                    break;

                case Monster.DreamDevourer:
                    switch(CurrentAction)
                    {
                        case MirAction.AttackRange1:
                            if (FrameIndex >= 3) Libraries.Monsters[(ushort)Monster.DreamDevourer].DrawBlend(320 + (FrameIndex + (int)Direction * 5) - 3, DrawLocation, Color.White, true);
                            break;
                    }
                    break;

                case Monster.TurtleKing:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)Monster.TurtleKing].DrawBlend(456 + FrameIndex + (int)Direction * 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Walking:
                            Libraries.Monsters[(ushort)Monster.TurtleKing].DrawBlend(488 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.TurtleKing].DrawBlend(536 + FrameIndex + (int)Direction * 10, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)Monster.TurtleKing].DrawBlend(616 + FrameIndex + (int)Direction * 2, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                        case MirAction.Revive:
                            Libraries.Monsters[(ushort)Monster.TurtleKing].DrawBlend(632 + FrameIndex + (int)Direction * 9, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack2:
                            Libraries.Monsters[(ushort)Monster.TurtleKing].DrawBlend(704 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.AttackRange1:
                            Libraries.Monsters[(ushort)Monster.TurtleKing].DrawBlend(752 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.AttackRange2:
                            Libraries.Monsters[(ushort)Monster.TurtleKing].DrawBlend(800 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.AttackRange3:
                            Libraries.Monsters[(ushort)Monster.TurtleKing].DrawBlend(848 + FrameIndex + (int)Direction * 8, DrawLocation, Color.White, true);
                            break;
                    }
                    break;

                case Monster.WingedTigerLord:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack1:
                            if (FrameIndex >= 2) Libraries.Monsters[(ushort)Monster.WingedTigerLord].DrawBlend(584 + (FrameIndex + (int)Direction * 6) - 2, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack2:
                            if (FrameIndex >= 2) Libraries.Monsters[(ushort)Monster.WingedTigerLord].DrawBlend(560 + (FrameIndex + (int)Direction * 3) - 2, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.StoningStatue:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack2:
                            Libraries.Monsters[(ushort)Monster.StoningStatue].DrawBlend(464 + FrameIndex + (int)Direction * 20, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.FlameSpear:
                case Monster.FlameMage:
                case Monster.FlameScythe:
                case Monster.FlameAssassin:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)BaseImage].DrawBlend(272 + FrameIndex + (int)Direction * 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Walking:
                            Libraries.Monsters[(ushort)BaseImage].DrawBlend(304 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)BaseImage].DrawBlend(352 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)BaseImage].DrawBlend(400 + FrameIndex + (int)Direction * 2, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                            Libraries.Monsters[(ushort)BaseImage].DrawBlend(416 + FrameIndex + (int)Direction * 10, DrawLocation, Color.White, true);
                            break;
                        case MirAction.AttackRange1:
                            Libraries.Monsters[(ushort)BaseImage].DrawBlend(496 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            if (BaseImage == Monster.FlameScythe && (int)Direction > 0)
                            {
                                Libraries.Monsters[(ushort)BaseImage].DrawBlend(544 + FrameIndex + (int)Direction * 6 - 6, DrawLocation, Color.White, true);
                            }
                            else if (BaseImage == Monster.FlameAssassin)
                            {
                                Libraries.Monsters[(ushort)BaseImage].DrawBlend(544 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            }
                            break;
                    }
                    break;
                case Monster.FlameQueen:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)Monster.FlameQueen].DrawBlend(360 + FrameIndex + (int)Direction * 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Walking:
                            Libraries.Monsters[(ushort)Monster.FlameQueen].DrawBlend(392 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)Monster.FlameQueen].DrawBlend(440 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)Monster.FlameQueen].DrawBlend(488 + FrameIndex + (int)Direction * 2, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                            Libraries.Monsters[(ushort)Monster.FlameQueen].DrawBlend(504 + FrameIndex + (int)Direction * 10, DrawLocation, Color.White, true);
                            break;
                        case MirAction.AttackRange1:
                            Libraries.Monsters[(ushort)Monster.FlameQueen].DrawBlend(584 + FrameIndex + (int)Direction * 9, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.HellKnight1:
                case Monster.HellKnight2:
                case Monster.HellKnight3:
                case Monster.HellKnight4:
                    switch (CurrentAction)
                    {
                        case MirAction.Appear:
                            Libraries.Monsters[(ushort)BaseImage].DrawBlend(224 + FrameIndex + (int)Direction * 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Standing:
                            Libraries.Monsters[(ushort)BaseImage].DrawBlend(224 + FrameIndex + (int)Direction * 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Walking:
                            Libraries.Monsters[(ushort)BaseImage].DrawBlend(256 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack1:
                            Libraries.Monsters[(ushort)BaseImage].DrawBlend(304 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)BaseImage].DrawBlend(352 + FrameIndex + (int)Direction * 2, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                            Libraries.Monsters[(ushort)BaseImage].DrawBlend(368 + FrameIndex + (int)Direction * 4, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Attack2:
                            Libraries.Monsters[(ushort)BaseImage].DrawBlend(400 + FrameIndex + (int)Direction * 6, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.HellLord:
                    switch (CurrentAction)
                    {
                        case MirAction.Standing:
                        case MirAction.Attack1:
                        case MirAction.Struck:
                            Libraries.Monsters[(ushort)Monster.HellLord].Draw(15, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Die:
                            Libraries.Monsters[(ushort)Monster.HellLord].Draw(16 + FrameIndex, DrawLocation, Color.White, true);
                            break;
                        case MirAction.Dead:
                            Libraries.Monsters[(ushort)Monster.HellLord].Draw(20, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.WaterGuard:
                    switch (CurrentAction)
                    {
                        case MirAction.Attack2:
                            if (FrameIndex >= 4) Libraries.Monsters[(ushort)Monster.WaterGuard].DrawBlend(264 + (FrameIndex + (int)Direction * 3) - 4, DrawLocation, Color.White, true);
                            break;
                    }
                    break;
                case Monster.Jar2:
                    if(CurrentAction!= MirAction.Dead)
                    {
                        Libraries.Monsters[(ushort)Monster.Jar2].DrawBlend(312 + (FrameIndex + (int)Direction * 10), DrawLocation, Color.White, true);
                        Libraries.Monsters[(ushort)Monster.Jar2].DrawBlend(382 + (FrameIndex + (int)Direction * 6), DrawLocation, Color.White, true);
                    }
                    if (CurrentAction == MirAction.Struck)
                    {
                        Libraries.Monsters[(ushort)Monster.Jar2].DrawBlend(520 + (FrameIndex + (int)Direction * 3), DrawLocation, Color.White, true);
                        //if (FrameIndex >= 2) Libraries.Monsters[(ushort)Monster.Jar2].DrawBlend(624 + (FrameIndex + (int)Direction * 8) - 2, DrawLocation, Color.White, true);
                    }
                    if (CurrentAction == MirAction.Die)
                    {
                        Libraries.Monsters[(ushort)Monster.Jar2].DrawBlend(544 + (FrameIndex + (int)Direction * 10), DrawLocation, Color.White, true);
                    }

                    break;

                case Monster.OmaWitchDoctor://奥玛巫医
                    if (CurrentAction != MirAction.Dead)
                    {
                        Libraries.Monsters[(ushort)Monster.OmaWitchDoctor].DrawBlend(400 + (FrameIndex + (int)Direction * 10), DrawLocation, Color.White, true);
                        //Libraries.Monsters[(ushort)Monster.OmaWitchDoctor].DrawBlend(480 + (FrameIndex + (int)Direction * 4), DrawLocation, Color.White, true);
                    }
                    if (CurrentAction == MirAction.Struck)
                    {
                        //Libraries.Monsters[(ushort)Monster.Jar2].DrawBlend(520 + (FrameIndex + (int)Direction * 3), DrawLocation, Color.White, true);
                        //if (FrameIndex >= 2) Libraries.Monsters[(ushort)Monster.Jar2].DrawBlend(624 + (FrameIndex + (int)Direction * 8) - 2, DrawLocation, Color.White, true);
                    }
                    if (CurrentAction == MirAction.Die)
                    {
                        //Libraries.Monsters[(ushort)Monster.Jar2].DrawBlend(544 + (FrameIndex + (int)Direction * 10), DrawLocation, Color.White, true);
                    }

                    break;

                case Monster.ClawBeast://水手长(2种形态，攻击手段不一样的)
                    if (CurrentAction != MirAction.Dead)
                    {
                        if (Stage == 0)
                        {
                            Libraries.Monsters[(ushort)Monster.ClawBeast].DrawBlend(654 + (FrameIndex + (int)Direction * 6), DrawLocation, Color.White, true);
                        }
                        if (Stage == 1)
                        {
                            Libraries.Monsters[(ushort)Monster.ClawBeast].DrawBlend(711 + (FrameIndex + (int)Direction * 2), DrawLocation, Color.White, true);
                        }
                    }
                    break;

              

                case Monster.CatShaman:
                    if (CurrentAction != MirAction.Dead)
                    {
                        Libraries.Monsters[(ushort)Monster.CatShaman].DrawBlend(360 + (FrameIndex + (int)Direction * 8), DrawLocation, Color.White, true);
                        Libraries.Monsters[(ushort)Monster.CatShaman].DrawBlend(424 + (FrameIndex + (int)Direction * 6), DrawLocation, Color.White, true);
                    }
              

                    break;
            }


        }

        public override void DrawName()
        {
            //是否显示怪物名称
            if (!GameScene.UserSet.ShowMonName)
            {
                return;
            }
            if (!Name.Contains("_"))
            {
                base.DrawName();
                return;
            }
            
            string[] splitName = Name.Split('_');

            //IntelligentCreature
            int yOffset = 0;
            switch (BaseImage)
            {
                case Monster.Chick:
                    yOffset = -10;
                    break;
                case Monster.BabyPig:
                case Monster.Kitten:
                case Monster.BabySkeleton:
                case Monster.Baekdon:
                case Monster.Wimaen:
                case Monster.BlackKitten:
                case Monster.BabyDragon:
                case Monster.OlympicFlame:
                case Monster.BabySnowMan:
                case Monster.Frog:
                case Monster.BabyMonkey:
                case Monster.AngryBird:
                case Monster.Foxey:
                    yOffset = -20;
                    break;
            }

            for (int s = 0; s < splitName.Count(); s++)
            {
                CreateMonsterLabel(splitName[s], s);

                TempLabel.Text = splitName[s];
                TempLabel.Location = new Point(DisplayRectangle.X + (48 - TempLabel.Size.Width) / 2, DisplayRectangle.Y - (32 - TempLabel.Size.Height / 2) + (Dead ? 35 : 8) - (((splitName.Count() - 1) * 10) / 2) + (s * 12) + yOffset);
                TempLabel.Draw();
            }
        }

        public void CreateMonsterLabel(string word, int wordOrder)
        {
            TempLabel = null;

            for (int i = 0; i < LabelList.Count; i++)
            {
                if (LabelList[i].Text != word) continue;
                TempLabel = LabelList[i];
                break;
            }

            if (TempLabel != null && !TempLabel.IsDisposed && NameColour == OldNameColor) return;

            OldNameColor = NameColour;

            TempLabel = new MirLabel
            {
                AutoSize = true,
                BackColour = Color.Transparent,
                ForeColour = NameColour,
                OutLine = true,
                OutLineColour = Color.Black,
                Text = word,
            };

            TempLabel.Disposing += (o, e) => LabelList.Remove(TempLabel);
            LabelList.Add(TempLabel);
        }

        public override void DrawChat()
        {
            if (ChatLabel == null || ChatLabel.IsDisposed) return;

            if (CMain.Time > ChatTime)
            {
                ChatLabel.Dispose();
                ChatLabel = null;
                return;
            }

            //IntelligentCreature
            int yOffset = 0;
            switch (BaseImage)
            {
                case Monster.Chick:
                    yOffset = 30;
                    break;
                case Monster.BabyPig:
                case Monster.Kitten:
                case Monster.BabySkeleton:
                case Monster.Baekdon:
                case Monster.Wimaen:
                case Monster.BlackKitten:
                case Monster.BabyDragon:
                case Monster.OlympicFlame:
                case Monster.BabySnowMan:
                case Monster.Frog:
                case Monster.BabyMonkey:
                case Monster.AngryBird:
                case Monster.Foxey:
                    yOffset = 20;
                    break;
            }

            ChatLabel.ForeColour = Dead ? Color.Gray : Color.White;
            ChatLabel.Location = new Point(DisplayRectangle.X + (48 - ChatLabel.Size.Width) / 2, DisplayRectangle.Y - (60 + ChatLabel.Size.Height) - (Dead ? 35 : 0) + yOffset);
            ChatLabel.Draw();
        }
    }
}
