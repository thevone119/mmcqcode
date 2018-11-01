using Client.MirGraphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Test : Form
    {
        public Test()
        {
            InitializeComponent();
        }
        //
        public List<int> IndexList = new List<int>();
        public List<MImage> Images = new List<MImage>();

        public short Width, Height, X, Y, ShadowX, ShadowY;

        private void button3_Click(object sender, EventArgs e)
        {

        }

        public byte Shadow;
        public int Length;
        //layer 2:
        public short MaskWidth, MaskHeight, MaskX, MaskY;

        private void Test_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

            //1.wtl：如果是wtl格式的文件，前面 _fStream.Seek(28, SeekOrigin.Begin);，28-32是总数，后面的就是索引

            //1.wzx是索引文件,格式为：前面44个字节是文件头，存放www.shandagames.com这种类似的，44-48个字节存放的是总数,48以后每4个都存放着一个索引位置,但是这个总数不一定对？这个太坑了
            //2.wzl是实际的资源文件
            string filename = @".\Data\mon11.wzl";
            string fileidx =  Path.ChangeExtension(filename, null) + ".wzx";
            if (!File.Exists(fileidx))
                return;
            FileStream _stream = new FileStream(fileidx, FileMode.Open, FileAccess.ReadWrite);
            BinaryReader _reader = new BinaryReader(_stream);
            //位移44个字节
            _stream.Seek(44, SeekOrigin.Begin);
            int count = _reader.ReadInt32();
            //这个总数据有可能错误，不用这个总数
            while(_reader.BaseStream.Position<= _reader.BaseStream.Length - 4)
            {
                int idx = _reader.ReadInt32();
                MirLog.info("idx:" + idx);
                IndexList.Add(_reader.ReadInt32());
                Images.Add(null);
            }
            
            _stream.Close();
            _reader.Close();

            //读取资源
            _stream = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite);
            _reader = new BinaryReader(_stream);
            _stream.Position = IndexList[2];
            
            for (int i = 0; i < 2; i++)
            {
                _stream.Position = IndexList[i];
                Width = _reader.ReadInt16();
                Height = _reader.ReadInt16();
                X = _reader.ReadInt16();
                Y = _reader.ReadInt16();
                ShadowX = _reader.ReadInt16();
                ShadowY = _reader.ReadInt16();
                Shadow = _reader.ReadByte();
                Length = _reader.ReadInt32();
                //byte[] FBytes = _reader.ReadBytes(Length);
                MirLog.info("Length:" + Length);
                //pictureBox1.Image = System.Drawing.Image.FromStream(new MemoryStream(FBytes));
                bool HasMask = ((Shadow >> 7) == 1) ? true : false;
                if (HasMask)
                {
                    MaskWidth = _reader.ReadInt16();
                    MaskHeight = _reader.ReadInt16();
                    MaskX = _reader.ReadInt16();
                    MaskY = _reader.ReadInt16();
                    int MaskLength = _reader.ReadInt32();
                    MirLog.info("MaskLength:" + MaskLength);
                    //MaskFBytes = _reader.ReadBytes(MaskLength);
                }
                MirLog.info("MaskWidth:" + MaskWidth);
                MirLog.info("MaskHeight:" + MaskHeight);
            }
  

           

        }
    }
}
