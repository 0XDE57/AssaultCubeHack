namespace AssaultCubeHack
{
  partial class AssaultHack
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
      this.picBoxOverlay = new System.Windows.Forms.PictureBox();
      ((System.ComponentModel.ISupportInitialize)(this.picBoxOverlay)).BeginInit();
      this.SuspendLayout();
      // 
      // picBoxOverlay
      // 
      this.picBoxOverlay.BackColor = System.Drawing.SystemColors.Control;
      this.picBoxOverlay.Location = new System.Drawing.Point(12, 12);
      this.picBoxOverlay.Name = "picBoxOverlay";
      this.picBoxOverlay.Size = new System.Drawing.Size(137, 123);
      this.picBoxOverlay.TabIndex = 0;
      this.picBoxOverlay.TabStop = false;
      // 
      // AssaultHack
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.BackColor = System.Drawing.SystemColors.Control;
      this.ClientSize = new System.Drawing.Size(502, 345);
      this.Controls.Add(this.picBoxOverlay);
      this.Name = "AssaultHack";
      this.ShowIcon = false;
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
      this.Text = "AssaultHack";
      this.TopMost = true;
      this.TransparencyKey = System.Drawing.Color.Black;
      this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AssaultHack_FormClosing);
      this.Load += new System.EventHandler(this.AssaultHack_Load);
      ((System.ComponentModel.ISupportInitialize)(this.picBoxOverlay)).EndInit();
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.PictureBox picBoxOverlay;
  }
}