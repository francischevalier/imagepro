using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.IO;

namespace ImagePro
{
    public class ClImage
    {
        public ClImage(string p_chemin)
        {
            Chemin = p_chemin;
            NomFichier = p_chemin.Substring(p_chemin.LastIndexOf('\\') + 1);
            NomAffiché = true;
            InfosAffichées = NomFichier;
            Image objÉlément = Image.FromFile(Chemin);
            TailleEnPixels = new Point(objÉlément.Width, objÉlément.Height);
            TaillesImage = (new FileInfo(Chemin).Length / 1024).ToString() +
                            "ko - " + TailleEnPixels.X + "x" + TailleEnPixels.Y + "px";
            objÉlément.Dispose();
        }

        public string Chemin { get; set; }
        public string NomFichier { get; set; }
        public bool NomAffiché { get; set; }
        public string InfosAffichées { get; set; }
        public Point TailleEnPixels { get; set; }
        public string TaillesImage { get; set; }
        public Point Position { get; set; }
    }
}