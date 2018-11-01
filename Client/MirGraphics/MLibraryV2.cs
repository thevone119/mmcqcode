using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
/// <summary>
/// 重写所有Mlibrary的方法
/// 让其支持直接加载传奇的资源
/// 目前以176版本的传奇资源为参考
/// </summary>
namespace Client.MirGraphics
{
    /// <summary>
    /// 传奇的资源文件加载器，主要是data目录下的资源，包括装备，首饰，界面，小地图等资源加载
    /// Hum.wil 衣服外观
    //  Weapon.wil 武器外观
    //HumEffect.wil 翅膀外观
    //ChrSel.wil 游戏登陆界面
    //Effect.wil 门的动作外观
    //Hair.wil 头发动作外观
    //Magic.wil 魔法效果外观1
    //Magic2.wil 魔法效果外观2
    //mmap.wil 小地图显示外观
    //Mon1.wil--Mon18.wil 怪物动作外观
    //npc.wil NPC动作外观
    //Objects.WIX--Objects7.WIX 大地图图库
    //Prguse.wil 游戏里的界面外观1
    //Prguse2.wil 游戏里的界面外观1
    //SmTiles.wil 地图图库
    //Tiles.wil 地面外观Tiles
    /// 
    /// </summary>

    public static class LibrariesV2
    {
        public static bool Loaded;
        public static int Count, Progress;

        public static readonly MLibrary
            ChrSel = new MLibrary(Settings.DataPath + "ChrSel"),//开门，并且选择英雄,传奇的资源和这个不一样？坑
            Prguse = new MLibrary(Settings.DataPath + "Prguse"),
            Prguse2 = new MLibrary(Settings.DataPath + "Prguse2"),
            Prguse3 = new MLibrary(Settings.DataPath + "Prguse3"),
            MiniMap = new MLibrary(Settings.DataPath + "MMap"),
            Magic = new MLibrary(Settings.DataPath + "Magic"),//魔法效果
            Magic2 = new MLibrary(Settings.DataPath + "Magic2"),//魔法效果
            Magic3 = new MLibrary(Settings.DataPath + "Magic3"),//魔法效果
            Effect = new MLibrary(Settings.DataPath + "Effect");



        public static readonly MLibrary
            Dragon = new MLibrary(Settings.DataPath + "Dragon");//掉落物品

        //Map
        public static readonly MLibrary[] MapLibs = new MLibrary[400];

        //Items
        public static readonly MLibrary
            Items = new MLibrary(Settings.DataPath + "Items"),
            StateItems = new MLibrary(Settings.DataPath + "StateItem"),
            FloorItems = new MLibrary(Settings.DataPath + "DNItems");

        //Deco
        public static readonly MLibrary
            Deco = new MLibrary(Settings.DataPath + "Deco");

        public static readonly MLibrary[] CArmours = new MLibrary[42],
                                          CWeapons = new MLibrary[55],
                                          CWeaponEffect = new MLibrary[67],
                                          CHair = new MLibrary[9],
                                          CHumEffect = new MLibrary[6],
                                          AArmours = new MLibrary[17],
                                          AWeaponsL = new MLibrary[14],
                                          AWeaponsR = new MLibrary[14],
                                          AHair = new MLibrary[9],
                                          AHumEffect = new MLibrary[3],
                                          ARArmours = new MLibrary[17],
                                          ARWeapons = new MLibrary[19],
                                          ARWeaponsS = new MLibrary[19],
                                          ARHair = new MLibrary[9],
                                          ARHumEffect = new MLibrary[3],
                                          Monsters = new MLibrary[406],
                                          Gates = new MLibrary[2],
                                          Flags = new MLibrary[12],
                                          Mounts = new MLibrary[12],
                                          NPCs = new MLibrary[200],
                                          Fishing = new MLibrary[2],
                                          Pets = new MLibrary[14],
                                          Transform = new MLibrary[28],
                                          TransformMounts = new MLibrary[28],
                                          TransformEffect = new MLibrary[2],
                                          TransformWeaponEffect = new MLibrary[1];

        static LibrariesV2()
        {
            //Wiz/War/Tao
            for (int i = 0; i < CArmours.Length; i++)
                CArmours[i] = new MLibrary(Settings.CArmourPath + i.ToString("00"));

            for (int i = 0; i < CHair.Length; i++)
                CHair[i] = new MLibrary(Settings.CHairPath + i.ToString("00"));

            for (int i = 0; i < CWeapons.Length; i++)
                CWeapons[i] = new MLibrary(Settings.CWeaponPath + i.ToString("00"));

            for (int i = 0; i < CWeaponEffect.Length; i++)
                CWeaponEffect[i] = new MLibrary(Settings.CWeaponEffectPath + i.ToString("00"));

            for (int i = 0; i < CHumEffect.Length; i++)
                CHumEffect[i] = new MLibrary(Settings.CHumEffectPath + i.ToString("00"));

            //Assassin
            for (int i = 0; i < AArmours.Length; i++)
                AArmours[i] = new MLibrary(Settings.AArmourPath + i.ToString("00"));

            for (int i = 0; i < AHair.Length; i++)
                AHair[i] = new MLibrary(Settings.AHairPath + i.ToString("00"));

            for (int i = 0; i < AWeaponsL.Length; i++)
                AWeaponsL[i] = new MLibrary(Settings.AWeaponPath + i.ToString("00") + " L");

            for (int i = 0; i < AWeaponsR.Length; i++)
                AWeaponsR[i] = new MLibrary(Settings.AWeaponPath + i.ToString("00") + " R");

            for (int i = 0; i < AHumEffect.Length; i++)
                AHumEffect[i] = new MLibrary(Settings.AHumEffectPath + i.ToString("00"));

            //Archer
            for (int i = 0; i < ARArmours.Length; i++)
                ARArmours[i] = new MLibrary(Settings.ARArmourPath + i.ToString("00"));

            for (int i = 0; i < ARHair.Length; i++)
                ARHair[i] = new MLibrary(Settings.ARHairPath + i.ToString("00"));

            for (int i = 0; i < ARWeapons.Length; i++)
                ARWeapons[i] = new MLibrary(Settings.ARWeaponPath + i.ToString("00"));

            for (int i = 0; i < ARWeaponsS.Length; i++)
                ARWeaponsS[i] = new MLibrary(Settings.ARWeaponPath + i.ToString("00") + " S");

            for (int i = 0; i < ARHumEffect.Length; i++)
                ARHumEffect[i] = new MLibrary(Settings.ARHumEffectPath + i.ToString("00"));

            //Other
            for (int i = 0; i < Monsters.Length; i++)
                Monsters[i] = new MLibrary(Settings.MonsterPath + i.ToString("000"));

            for (int i = 0; i < Gates.Length; i++)
                Gates[i] = new MLibrary(Settings.GatePath + i.ToString("00"));

            for (int i = 0; i < Flags.Length; i++)
                Flags[i] = new MLibrary(Settings.FlagPath + i.ToString("00"));

            for (int i = 0; i < NPCs.Length; i++)
                NPCs[i] = new MLibrary(Settings.NPCPath + i.ToString("00"));

            for (int i = 0; i < Mounts.Length; i++)
                Mounts[i] = new MLibrary(Settings.MountPath + i.ToString("00"));

            for (int i = 0; i < Fishing.Length; i++)
                Fishing[i] = new MLibrary(Settings.FishingPath + i.ToString("00"));

            for (int i = 0; i < Pets.Length; i++)
                Pets[i] = new MLibrary(Settings.PetsPath + i.ToString("00"));

            for (int i = 0; i < Transform.Length; i++)
                Transform[i] = new MLibrary(Settings.TransformPath + i.ToString("00"));

            for (int i = 0; i < TransformMounts.Length; i++)
                TransformMounts[i] = new MLibrary(Settings.TransformMountsPath + i.ToString("00"));

            for (int i = 0; i < TransformEffect.Length; i++)
                TransformEffect[i] = new MLibrary(Settings.TransformEffectPath + i.ToString("00"));

            for (int i = 0; i < TransformWeaponEffect.Length; i++)
                TransformWeaponEffect[i] = new MLibrary(Settings.TransformWeaponEffectPath + i.ToString("00"));

            #region Maplibs
            //wemade mir2 (allowed from 0-99)
            MapLibs[0] = new MLibrary(Settings.DataPath + "Map\\WemadeMir2\\Tiles");
            MapLibs[1] = new MLibrary(Settings.DataPath + "Map\\WemadeMir2\\Smtiles");
            MapLibs[2] = new MLibrary(Settings.DataPath + "Map\\WemadeMir2\\Objects");
            for (int i = 2; i < 24; i++)
            {
                MapLibs[i + 1] = new MLibrary(Settings.DataPath + "Map\\WemadeMir2\\Objects" + i.ToString());
            }
            //shanda mir2 (allowed from 100-199)
            MapLibs[100] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\Tiles");
            for (int i = 1; i < 10; i++)
            {
                MapLibs[100 + i] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\Tiles" + (i + 1));
            }
            MapLibs[110] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\SmTiles");
            for (int i = 1; i < 10; i++)
            {
                MapLibs[110 + i] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\SmTiles" + (i + 1));
            }
            MapLibs[120] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\Objects");
            for (int i = 1; i < 31; i++)
            {
                MapLibs[120 + i] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\Objects" + (i + 1));
            }
            MapLibs[190] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\AniTiles1");
            //wemade mir3 (allowed from 200-299)
            string[] Mapstate = { "", "wood\\", "sand\\", "snow\\", "forest\\" };
            for (int i = 0; i < Mapstate.Length; i++)
            {
                MapLibs[200 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Tilesc");
                MapLibs[201 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Tiles30c");
                MapLibs[202 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Tiles5c");
                MapLibs[203 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Smtilesc");
                MapLibs[204 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Housesc");
                MapLibs[205 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Cliffsc");
                MapLibs[206 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Dungeonsc");
                MapLibs[207 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Innersc");
                MapLibs[208 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Furnituresc");
                MapLibs[209 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Wallsc");
                MapLibs[210 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "smObjectsc");
                MapLibs[211 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Animationsc");
                MapLibs[212 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Object1c");
                MapLibs[213 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Object2c");
            }
            Mapstate = new string[] { "", "wood", "sand", "snow", "forest" };
            //shanda mir3 (allowed from 300-399)
            for (int i = 0; i < Mapstate.Length; i++)
            {
                MapLibs[300 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Tilesc" + Mapstate[i]);
                MapLibs[301 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Tiles30c" + Mapstate[i]);
                MapLibs[302 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Tiles5c" + Mapstate[i]);
                MapLibs[303 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Smtilesc" + Mapstate[i]);
                MapLibs[304 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Housesc" + Mapstate[i]);
                MapLibs[305 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Cliffsc" + Mapstate[i]);
                MapLibs[306 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Dungeonsc" + Mapstate[i]);
                MapLibs[307 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Innersc" + Mapstate[i]);
                MapLibs[308 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Furnituresc" + Mapstate[i]);
                MapLibs[309 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Wallsc" + Mapstate[i]);
                MapLibs[310 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "smObjectsc" + Mapstate[i]);
                MapLibs[311 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Animationsc" + Mapstate[i]);
                MapLibs[312 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Object1c" + Mapstate[i]);
                MapLibs[313 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Object2c" + Mapstate[i]);
            }
            #endregion

            LoadLibraries();

            Thread thread = new Thread(LoadGameLibraries) { IsBackground = true };
            thread.Start();
        }

        static void LoadLibraries()
        {
            ChrSel.Initialize();
            Progress++;

            Prguse.Initialize();
            Progress++;
            MirLog.debug("Prguse 68 Height:" + Prguse.GetSize(68).Height);
            MirLog.debug("Prguse 16 Height:" + Prguse.GetSize(16).Height);
            MirLog.debug("Prguse 1 Height:" + Prguse.GetSize(1).Height);
            MirLog.debug("Prguse 69 Height:" + Prguse.GetSize(69).Height);


            Prguse2.Initialize();
            Progress++;

            Prguse3.Initialize();
            Progress++;
        }

        private static void LoadGameLibraries()
        {
            Count = MapLibs.Length + Monsters.Length + Gates.Length + NPCs.Length + CArmours.Length +
                CHair.Length + CWeapons.Length + CWeaponEffect.Length + AArmours.Length + AHair.Length + AWeaponsL.Length + AWeaponsR.Length +
                ARArmours.Length + ARHair.Length + ARWeapons.Length + ARWeaponsS.Length +
                CHumEffect.Length + AHumEffect.Length + ARHumEffect.Length + Mounts.Length + Fishing.Length + Pets.Length +
                Transform.Length + TransformMounts.Length + TransformEffect.Length + TransformWeaponEffect.Length + 17;

            Dragon.Initialize();
            Progress++;



            MiniMap.Initialize();
            Progress++;



            Magic.Initialize();
            Progress++;
            Magic2.Initialize();
            Progress++;
            Magic3.Initialize();
            Progress++;


            Effect.Initialize();
            Progress++;


            Deco.Initialize();
            Progress++;

            Items.Initialize();
            Progress++;
            StateItems.Initialize();
            Progress++;
            FloorItems.Initialize();
            Progress++;

            for (int i = 0; i < MapLibs.Length; i++)
            {
                if (MapLibs[i] == null)
                    MapLibs[i] = new MLibrary("");
                else
                    MapLibs[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Monsters.Length; i++)
            {
                Monsters[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Gates.Length; i++)
            {
                Gates[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < NPCs.Length; i++)
            {
                NPCs[i].Initialize();
                Progress++;
            }


            for (int i = 0; i < CArmours.Length; i++)
            {
                CArmours[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < CHair.Length; i++)
            {
                CHair[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < CWeapons.Length; i++)
            {
                CWeapons[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < CWeaponEffect.Length; i++)
            {
                CWeaponEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AArmours.Length; i++)
            {
                AArmours[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AHair.Length; i++)
            {
                AHair[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AWeaponsL.Length; i++)
            {
                AWeaponsL[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AWeaponsR.Length; i++)
            {
                AWeaponsR[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARArmours.Length; i++)
            {
                ARArmours[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARHair.Length; i++)
            {
                ARHair[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARWeapons.Length; i++)
            {
                ARWeapons[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARWeaponsS.Length; i++)
            {
                ARWeaponsS[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < CHumEffect.Length; i++)
            {
                CHumEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AHumEffect.Length; i++)
            {
                AHumEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARHumEffect.Length; i++)
            {
                ARHumEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Mounts.Length; i++)
            {
                Mounts[i].Initialize();
                Progress++;
            }


            for (int i = 0; i < Fishing.Length; i++)
            {
                Fishing[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Pets.Length; i++)
            {
                Pets[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Transform.Length; i++)
            {
                Transform[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < TransformEffect.Length; i++)
            {
                TransformEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < TransformWeaponEffect.Length; i++)
            {
                TransformWeaponEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < TransformMounts.Length; i++)
            {
                TransformMounts[i].Initialize();
                Progress++;
            }

            Loaded = true;
        }

    }


    //传奇资源的加载器
    public class MirLibrary
    {
        //文件名,文件的绝对路径
        private string fileName;
        //文件索引路径
        private string fidxName;
        //是否加载
        private bool _initialized;
        //当前文件类型
        public byte _nType = 0; //0 = .wil //1 = .wzl //2 = .wil new wemade design //3 = .wil mir3 //4 = .miz shanda mir3

        private static byte[] ImageStructureSize = { 8, 16, 16, 17, 16 };//base size of an image structure,不同版本跳过的数,这个其实可以用静态，不过意义也不太大？
        //版本号？针对wil格式的文件，有版本号
        private int _version = 0;
        //文件索引数据会读取到这里
        private List<int> _indexList;

        //实际的图像数据存储在这里
        public WeMadeImage[] Images;
        //色板?
        private int[] _palette;
        //文件IO
        private BinaryReader _bReader;
        private FileStream _fStream;

        //图像的总数
        #region count
        private int _count;
        public int count
        {
            get { return _count; }
            set
            {
                if (_count == value)
                    return;
            }
        }
        #endregion


        public MirLibrary(string fileName)
        {
            //判断文件是否存在
            this.fileName = fileName;
            if (this.fileName == null)
            {
                return;
            }
            if (File.Exists(fileName))
            {
                return;
            }
            fidxName = Path.ChangeExtension(fileName, "Wix");
            //判断文件类型
            switch (Path.GetExtension(fileName).ToLower())
            {
                case ".wzl":
                    _nType = 1;
                    fidxName = Path.ChangeExtension(fileName, "Wzx");
                    break;
                case ".miz":
                    _nType = 4;
                    fidxName = Path.ChangeExtension(fileName, "Mix");
                    break;
            }
            //不存在索引
            if (File.Exists(fidxName))
            {
                return;
            }
            //加载数据
            Initialize();
        }


        //加载数据，只加载index索引数据
        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            
            _initialized = true;
            //读取主文件
            _fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            _bReader = new BinaryReader(_fStream);
            //加载色板，版本等信息
            byte[] buffer;
            _palette = new int[256] { -16777216, -8388608, -16744448, -8355840, -16777088, -8388480, -16744320, -4144960, -11173737, -6440504, -8686733, -13817559, -10857902, -10266022, -12437191, -14870504, -15200240, -14084072, -15726584, -886415, -2005153, -42406, -52943, -2729390, -7073792, -7067368, -13039616, -9236480, -4909056, -4365486, -12445680, -21863, -10874880, -9225943, -5944783, -7046285, -4369871, -11394800, -8703720, -13821936, -7583183, -7067392, -4378368, -3771566, -9752296, -3773630, -3257856, -5938375, -10866408, -14020608, -15398912, -12969984, -16252928, -14090240, -11927552, -6488064, -2359296, -2228224, -327680, -6524078, -7050422, -9221591, -11390696, -7583208, -7846895, -11919104, -14608368, -2714534, -3773663, -1086720, -35072, -5925756, -12439263, -15200248, -14084088, -14610432, -13031144, -7576775, -12441328, -9747944, -8697320, -7058944, -7568261, -9739430, -11910599, -14081768, -12175063, -4872812, -8688806, -3231340, -5927821, -7572646, -4877197, -2710157, -1071798, -1063284, -8690878, -9742791, -4352934, -10274560, -2701651, -11386327, -7052520, -1059155, -5927837, -10266038, -4348549, -10862056, -4355023, -13291223, -7043997, -8688822, -5927846, -10859991, -6522055, -12439280, -1069791, -15200256, -14081792, -6526208, -7044006, -11386344, -9741783, -8690911, -6522079, -2185984, -10857927, -13555440, -3228293, -10266055, -7044022, -3758807, -15688680, -12415926, -13530046, -15690711, -16246768, -16246760, -16242416, -15187415, -5917267, -9735309, -15193815, -15187382, -13548982, -10238242, -12263937, -7547153, -9213127, -532935, -528500, -530688, -9737382, -10842971, -12995089, -11887410, -13531979, -13544853, -2171178, -4342347, -7566204, -526370, -16775144, -16246727, -16248791, -16246784, -16242432, -16756059, -16745506, -15718070, -15713941, -15707508, -14591323, -15716006, -15711612, -13544828, -15195855, -11904389, -11375707, -14075549, -15709474, -14079711, -11908551, -14079720, -11908567, -8684734, -6513590, -10855895, -12434924, -13027072, -10921728, -3525332, -9735391, -14077696, -13551344, -13551336, -12432896, -11377896, -10849495, -13546984, -15195904, -15191808, -15189744, -10255286, -9716406, -10242742, -10240694, -10838966, -11891655, -10238390, -10234294, -11369398, -13536471, -10238374, -11354806, -15663360, -15193832, -11892662, -11868342, -16754176, -16742400, -16739328, -16720384, -16716288, -16712960, -11904364, -10259531, -8680234, -9733162, -8943361, -3750194, -7039844, -6515514, -13553351, -14083964, -15204220, -11910574, -11386245, -10265997, -3230217, -7570532, -8969524, -2249985, -1002454, -2162529, -1894477, -1040, -6250332, -8355712, -65536, -16711936, -256, -16776961, -65281, -16711681, -1, };
            //如果_nType==0,判断_nType的实际类型
            if (_nType == 0) //at least we know it's a .wil file up to now
            {
                _fStream.Seek(0, SeekOrigin.Begin);
                buffer = _bReader.ReadBytes(48);
                _nType = (byte)(buffer[26] == 64 ? 2 : buffer[2] == 73 ? 3 : _nType);
                if (_nType == 0)
                {
                    _palette = new int[_bReader.ReadInt32()];
                    _fStream.Seek(4, SeekOrigin.Current);
                    _version = _bReader.ReadInt32();
                    _fStream.Seek(_version == 0 ? 0 : 4, SeekOrigin.Current);
                    for (int i = 1; i < _palette.Length; i++)
                        _palette[i] = _bReader.ReadInt32() + (255 << 24);
                }
            }
            //读取索引数据
            LoadIndexFile();
            //

        }

        //读取索引数据，加载索引wzx,wix
        private void LoadIndexFile()
        {
            _indexList = new List<int>();
            FileStream stream = null;
            try
            {
                stream = new FileStream(fidxName, FileMode.Open, FileAccess.Read);
                stream.Seek(0, SeekOrigin.Begin);
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    switch (_nType)
                    {
                        case 4:
                            stream.Seek(24, SeekOrigin.Begin);
                            break;

                        case 3:
                            reader.ReadBytes(26);
                            if (reader.ReadUInt16() != 0xB13A)
                                stream.Seek(24, SeekOrigin.Begin);
                            break;

                        case 2:
                            reader.ReadBytes(52);
                            break;

                        default:
                            reader.ReadBytes(_version == 0 ? 48 : 52);
                            break;
                    }

                    stream = null;
                    while (reader.BaseStream.Position <= reader.BaseStream.Length - 4)
                        _indexList.Add(reader.ReadInt32());
                }
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
            //总数
            this.count = _indexList.Count;
            Images = new WeMadeImage[count];
        }
        //重新加载图像
        private void CheckImage(int index)
        {
            if (Images == null || index < 0 || index >= Images.Length) return;
            if (Images[index] == null)
            {
                _fStream.Position = _indexList[index];
                Images[index] = new WeMadeImage(_bReader, _nType);
            }
            //根据不同版本加载不同的逻辑
            if (Images[index].Image == null)
            {
                _fStream.Seek(_indexList[index] + (_nType > 0 ? ImageStructureSize[_nType] : _version == 0 ? 8 : 12), SeekOrigin.Begin);
                Images[index].CreateTexture(_bReader, _palette);
            }
        }
        //返回图像
        public WeMadeImage getWeMadeImage(int index)
        {
            if (Images == null || index < 0 || index >= Images.Length) return null;
            CheckImage(index);
            return Images[index];
        }

    }

    //传奇的图片
    public class WeMadeImage
    {
        public readonly short Width, Height, X, Y, ShadowX, ShadowY;
        public Rectangle TrueSize;
        public Bitmap Image;
        //最后清除时间，10分钟清除一次？
        public long CleanTime;
        public byte nType = 0;//0 = .wil //1 = .wzl //2 = .wil new wemade design //3 = .wil mir3 //4 = .miz shanda mir3
        public bool bo16bit = false;
        public int nSize;//图像的大小，读取多少位
        //以下几个参数是针对mir3的图像的，阴影也放在这里
        public bool boHasShadow;//
        public bool HasMask;
        public Bitmap MaskImage;

        //16位色转成32位色？
        private int convert16bitTo32bit(int color)
        {
            byte red = (byte)((color & 0xf800) >> 8);
            byte green = (byte)((color & 0x07e0) >> 3);
            byte blue = (byte)((color & 0x001f) << 3);
            return ((red << 0x10) | (green << 0x8) | blue) | (255 << 24);//the final or is setting alpha to max so it'll display (since mir2 images have no alpha layer)
        }

        private int WidthBytes(int nBit, int nWidth)
        {
            return (((nWidth * nBit) + 31) >> 5) * 4;
        }

        //压缩成mir3?
        private byte[][] DecompressWemadeMir3(BinaryReader BReader, short OutputWidth, short OutputHeight, int InputLength)
        {
            byte[][] Pixels = new byte[2][];
            Pixels[0] = new byte[OutputWidth * OutputHeight * 2];
            Pixels[1] = new byte[OutputWidth * OutputHeight * 2];
            byte[] FileBytes = BReader.ReadBytes(InputLength * 2);

            int End = 0, OffSet = 0, Start = 0, Count;

            int nX, x = 0;
            //for (int Y = 0; Y < OutputHeight; Y++)
            for (int Y = OutputHeight - 1; Y >= 0; Y--)
            {
                OffSet = Start * 2;
                End += FileBytes[OffSet];
                Start++;
                nX = Start;
                OffSet += 2;
                while (nX < End)
                {
                    switch (FileBytes[OffSet])
                    {
                        case 192: //No Colour
                            nX += 2;
                            x += FileBytes[OffSet + 3] << 8 | FileBytes[OffSet + 2];
                            OffSet += 4;
                            break;

                        case 193:  //Solid Colour
                        case 195:
                            nX += 2;
                            Count = FileBytes[OffSet + 3] << 8 | FileBytes[OffSet + 2];
                            OffSet += 4;
                            for (int i = 0; i < Count; i++)
                            {
                                Pixels[0][(Y * OutputWidth + x) * 2] = FileBytes[OffSet];
                                Pixels[0][(Y * OutputWidth + x) * 2 + 1] = FileBytes[OffSet + 1];
                                OffSet += 2;
                                if (x >= OutputWidth) continue;
                                x++;
                            }
                            nX += Count;
                            break;

                        case 194:  //Overlay Colour
                            HasMask = true;
                            nX += 2;
                            Count = FileBytes[OffSet + 3] << 8 | FileBytes[OffSet + 2];
                            OffSet += 4;
                            for (int i = 0; i < Count; i++)
                            {
                                for (int j = 0; j < 2; j++)
                                {
                                    Pixels[j][(Y * OutputWidth + x) * 2] = FileBytes[OffSet];
                                    Pixels[j][(Y * OutputWidth + x) * 2 + 1] = FileBytes[OffSet + 1];
                                }
                                OffSet += 2;
                                if (x >= OutputWidth) continue;
                                x++;
                            }
                            nX += Count;
                            break;
                    }
                }
                End++;
                Start = End;
                x = 0;
            }
            return Pixels;
        }

        //读取,这里只读取文件的基础信息，并未读取真正的图片信息
        public WeMadeImage(BinaryReader reader, byte nType)
        {
            //从开始位置开始读
            if (reader.BaseStream.Position == 0) return;
            this.nType = nType;
            if (nType == 1)
            {
                bo16bit = (reader.ReadByte() == 5 ? true : false);
                reader.ReadBytes(3);
            }
            //读取文件的宽，高，x,y等信息
            Width = reader.ReadInt16();
            Height = reader.ReadInt16();
            X = reader.ReadInt16();
            Y = reader.ReadInt16();
            nSize = Width * Height;

            switch (nType)
            {
                case 1:
                    nSize = reader.ReadInt32();
                    break;

                case 4:
                    bo16bit = true;
                    nSize = reader.ReadInt32();
                    break;

                case 2:
                    bo16bit = true;
                    reader.ReadInt16();
                    reader.ReadInt16();
                    nSize = reader.ReadInt32();
                    Width = (nSize < 6) ? (short)0 : Width;
                    break;

                case 3:
                    bo16bit = true;
                    boHasShadow = reader.ReadByte() == 1 ? true : false;
                    ShadowX = reader.ReadInt16();
                    ShadowY = reader.ReadInt16();
                    nSize = reader.ReadInt32() * 2;
                    break;
            }
            Width = (nSize == 0) ? (short)0 : Width; //this makes sure blank images aren't being processed
        }

        //创建纹理，这个才是真正的读取图片信息
        public unsafe void CreateTexture(BinaryReader reader, int[] palette)
        {
            if (Width == 0 || Height == 0) return;
            Image = new Bitmap(Width, Height);
            MaskImage = new Bitmap(1, 1);

            BitmapData data = Image.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            byte[] bytes = new byte[0];
            byte[] maskbytes = new byte[0];
            MemoryStream output;
            switch (nType)
            {
                case 0://wemade wil file uncompressed
                    if (palette.Length > 256)
                    {
                        bo16bit = true;
                        nSize = nSize * 2;
                    }
                    bytes = reader.ReadBytes(nSize);
                    break;

                case 1://shanda wzl file compressed
                case 4://shanda miz file compressed(这个要用的zip的包，暂时不管，注释掉)
                    /**
                    output = new MemoryStream();
                    Ionic.Zlib.ZlibStream deflateStream = new Ionic.Zlib.ZlibStream(output, Ionic.Zlib.CompressionMode.Decompress);
                    deflateStream.Write(reader.ReadBytes(nSize), 0, nSize);
                    bytes = output.ToArray();
                    deflateStream.Close();
                    output.Close();
                    **/
                    break;

                case 2:
                    byte Compressed = reader.ReadByte();
                    reader.ReadBytes(5);
                    if (Compressed != 8)
                    {
                        bytes = reader.ReadBytes(nSize - 6);
                        break;
                    }
                    MemoryStream input = new MemoryStream(reader.ReadBytes(nSize - 6));
                    output = new MemoryStream();
                    byte[] buffer = new byte[10];
                    System.IO.Compression.DeflateStream decompress = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress);
                    int len;
                    while ((len = decompress.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        output.Write(buffer, 0, len);
                    }
                    bytes = output.ToArray();
                    decompress.Close();
                    output.Close();
                    input.Close();
                    break;

                case 3: //3 = .wil mir3
                    MaskImage = new Bitmap(Width, Height);
                    byte[][] DecodedPixels = DecompressWemadeMir3(reader, Width, Height, nSize);
                    if (DecodedPixels != null)
                    {
                        bytes = DecodedPixels[0];
                        if (HasMask)
                            maskbytes = DecodedPixels[1];
                    }
                    else
                    {
                        HasMask = false;
                        bytes = new byte[Width * Height * 2];
                    }
                    break;
            }
            int index = 0;

            int* scan0 = (int*)data.Scan0;
            {
                for (int y = Height - 1; y >= 0; y--)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        if (bo16bit)
                            scan0[y * Width + x] = convert16bitTo32bit(bytes[index++] + (bytes[index++] << 8));
                        else
                            scan0[y * Width + x] = palette[bytes[index++]];
                    }
                    if (((nType == 1) || (nType == 4)) & (Width % 4 > 0))
                        index += WidthBytes(bo16bit ? 16 : 8, Width) - (Width * (bo16bit ? 2 : 1));
                }
            }
            Image.UnlockBits(data);
            index = 0;
            if (HasMask)
            {
                BitmapData Maskdata = MaskImage.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                int* maskscan0 = (int*)Maskdata.Scan0;
                {
                    for (int y = Height - 1; y >= 0; y--)
                    {
                        for (int x = 0; x < Width; x++)
                            maskscan0[y * Width + x] = convert16bitTo32bit(maskbytes[index++] + (maskbytes[index++] << 8));
                    }
                }
                MaskImage.UnlockBits(Maskdata);
            }
        }
    }


}
