namespace Client
{
    partial class PayForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pay_title = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.lab_name = new System.Windows.Forms.Label();
            this.pic_title = new System.Windows.Forms.PictureBox();
            this.lab_oid = new System.Windows.Forms.Label();
            this.lab_time = new System.Windows.Forms.Label();
            this.pic_close = new System.Windows.Forms.PictureBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pic_title)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pic_close)).BeginInit();
            this.SuspendLayout();
            // 
            // pay_title
            // 
            this.pay_title.AutoSize = true;
            this.pay_title.BackColor = System.Drawing.Color.Transparent;
            this.pay_title.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pay_title.ForeColor = System.Drawing.Color.Orange;
            this.pay_title.Location = new System.Drawing.Point(117, 12);
            this.pay_title.Name = "pay_title";
            this.pay_title.Size = new System.Drawing.Size(138, 28);
            this.pay_title.TabIndex = 0;
            this.pay_title.Text = "微信扫码支付";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox1.Location = new System.Drawing.Point(70, 55);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(210, 210);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // lab_name
            // 
            this.lab_name.AutoSize = true;
            this.lab_name.BackColor = System.Drawing.Color.Transparent;
            this.lab_name.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lab_name.ForeColor = System.Drawing.Color.White;
            this.lab_name.Location = new System.Drawing.Point(68, 280);
            this.lab_name.Name = "lab_name";
            this.lab_name.Size = new System.Drawing.Size(142, 17);
            this.lab_name.TabIndex = 2;
            this.lab_name.Text = "商品名称：元宝充值10元";
            // 
            // pic_title
            // 
            this.pic_title.BackgroundImage = global::Client.Properties.Resources.wx_logo;
            this.pic_title.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pic_title.Location = new System.Drawing.Point(81, 9);
            this.pic_title.Name = "pic_title";
            this.pic_title.Size = new System.Drawing.Size(32, 32);
            this.pic_title.TabIndex = 4;
            this.pic_title.TabStop = false;
            // 
            // lab_oid
            // 
            this.lab_oid.AutoSize = true;
            this.lab_oid.BackColor = System.Drawing.Color.Transparent;
            this.lab_oid.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lab_oid.ForeColor = System.Drawing.Color.White;
            this.lab_oid.Location = new System.Drawing.Point(68, 310);
            this.lab_oid.Name = "lab_oid";
            this.lab_oid.Size = new System.Drawing.Size(196, 17);
            this.lab_oid.TabIndex = 5;
            this.lab_oid.Text = "订单号：10242451447454754142";
            // 
            // lab_time
            // 
            this.lab_time.AutoSize = true;
            this.lab_time.BackColor = System.Drawing.Color.Transparent;
            this.lab_time.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lab_time.ForeColor = System.Drawing.Color.White;
            this.lab_time.Location = new System.Drawing.Point(68, 340);
            this.lab_time.Name = "lab_time";
            this.lab_time.Size = new System.Drawing.Size(164, 17);
            this.lab_time.TabIndex = 6;
            this.lab_time.Text = "二维码有效期：0时 4分 59秒";
            // 
            // pic_close
            // 
            this.pic_close.BackColor = System.Drawing.Color.Transparent;
            this.pic_close.BackgroundImage = global::Client.Properties.Resources.close_base;
            this.pic_close.Location = new System.Drawing.Point(318, 11);
            this.pic_close.Name = "pic_close";
            this.pic_close.Size = new System.Drawing.Size(24, 21);
            this.pic_close.TabIndex = 7;
            this.pic_close.TabStop = false;
            this.pic_close.Click += new System.EventHandler(this.pic_close_Click);
            this.pic_close.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pic_close_MouseDown);
            this.pic_close.MouseEnter += new System.EventHandler(this.pic_close_MouseEnter);
            this.pic_close.MouseLeave += new System.EventHandler(this.pic_close_MouseLeave);
            this.pic_close.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pic_close_MouseUp);
            // 
            // timer1
            // 
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // PayForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImage = global::Client.Properties.Resources.bj_01;
            this.ClientSize = new System.Drawing.Size(350, 420);
            this.Controls.Add(this.pic_close);
            this.Controls.Add(this.lab_time);
            this.Controls.Add(this.lab_oid);
            this.Controls.Add(this.pic_title);
            this.Controls.Add(this.lab_name);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.pay_title);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "PayForm";
            this.ShowInTaskbar = false;
            this.Text = "二维码支付";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PayForm_FormClosing);
            this.Load += new System.EventHandler(this.PayForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pic_title)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pic_close)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label pay_title;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lab_name;
        private System.Windows.Forms.PictureBox pic_title;
        private System.Windows.Forms.Label lab_oid;
        private System.Windows.Forms.Label lab_time;
        private System.Windows.Forms.PictureBox pic_close;
        private System.Windows.Forms.Timer timer1;
    }
}