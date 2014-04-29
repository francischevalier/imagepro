using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace ImagePro
{
    public class ClDossier
    {
        public ClDossier(string p_chemin, ClDossier p_dossierParent)
        {
            Chemin = p_chemin;
            NomDossier = p_chemin.Substring(p_chemin.LastIndexOf('\\') + 1);
            SousDossier = (p_dossierParent != null) ? true : false;

            if (SousDossier)
                Parent = p_dossierParent;

            Images = new List<ClImage>();
            // Les extensions acceptées en tant qu'image.
            string[] extensionsImage = { ".jpg", ".png", ".jpeg", ".tif", ".bmp" };
            // On récupère tous les fichiers de ce dossier.
            string[] fichiers = Directory.GetFiles(Chemin);
            for (int i = 0; i < fichiers.Length; ++i)
            {
                if (extensionsImage.Contains(Path.GetExtension(fichiers[i])))
                    Images.Add(new ClImage(fichiers[i]));
            }
            // On récupère également tous les sous-dossiers de ce dossier.
            SousDossiers = new List<ClDossier>();
            string[] dossiers = Directory.GetDirectories(Chemin);
            for (int i = 0; i < dossiers.Length; ++i)
                SousDossiers.Add(new ClDossier(dossiers[i], this));

            // Le nb d'éléments à afficher au total
            NbÉléments = SousDossiers.Count + Images.Count;

            // On détermine le nombre d'éléments maximal par lignes et colonnes.
            // La taille minimal est de 200 pixels plus l'espace entre eux.
            int nbMaxColonnes = (m_largeurÉcran - BORDURE_CONTOUR * 2) / (200 + BORDURE_X);
            int nbMaxLignes = m_hauteurÉcran / (200 + BORDURE_Y);

            NbPages = (int)Math.Ceiling(NbÉléments / (double)(nbMaxColonnes * nbMaxLignes));
        }

        // Marge entre chaque image en pixels.
        const int BORDURE_CONTOUR = 75;
        const int BORDURE_X = 10;
        const int BORDURE_Y = 30;
        const int BORDURE_INFOS = 5;

        // L'espace disponible pour les images
        int m_hauteurÉcran = Screen.PrimaryScreen.WorkingArea.Height - 105;
        int m_largeurÉcran = Screen.PrimaryScreen.WorkingArea.Width - 150;

        public int ObtenirProchaineImage()
        {
            if (IndiceImage < (Images.Count - 1))
                ++IndiceImage;
            else
                IndiceImage = 0;

            return IndiceImage;
        }

        public int ObtenirImagePrécédente()
        {
            if (IndiceImage == 0)
                IndiceImage = Images.Count - 1;
            else
                --IndiceImage;

            return IndiceImage;                
        }

        public void FaireMiseEnPage()
        {
            // Le nb d'éléments par page, excepté la dernière qui peut être moins fournie
            NbÉlémentsPage = (int)Math.Ceiling(NbÉléments / (double)NbPages);


            if (NbÉlémentsPage > 0)
            {
                // On indique à partir de quel élément dans la liste on part les vignettes
                IndiceÉlément = IndicePage * NbÉlémentsPage;

                if (DernierePage())
                    NbÉlémentsPage = NbÉléments - (IndicePage * NbÉlémentsPage);

                // Déterminer si on dessine des dossiers ou des images, et à l'indice du dossier/image à dessiner
                if (SousDossiers.Count() > IndiceÉlément)
                {
                    IndiceDossier = IndiceÉlément;
                    IndiceImage = 0;
                }
                else
                    IndiceImage = IndiceÉlément - SousDossiers.Count();

                // On regarde ensuite la taille maximal pour chaque élément
                TailleÉlément = (int)Math.Sqrt(m_largeurÉcran * m_hauteurÉcran / NbÉlémentsPage);

                TailleÉlément -= BORDURE_Y;

                // On calcule combien d'éléments rentrent par colonne
                NbColonnes = (int)(m_largeurÉcran / TailleÉlément);

                // Ensuite, on regarde sur combien de lignes on doit étaler les éléments.
                NbLignes = (int)(Math.Ceiling((double)(NbÉlémentsPage) / NbColonnes));

                int tailleHorizontale = (m_largeurÉcran - (BORDURE_X * NbColonnes)) /
                          (NbColonnes + (int)Math.Ceiling(((NbÉlémentsPage - NbColonnes
                          * (NbLignes - 1)) / (double)(NbLignes - 1))));
                int tailleVerticale = m_hauteurÉcran / NbLignes;

                if (tailleHorizontale > tailleVerticale)
                {
                    NbLignes -= 1;
                    NbColonnes += (int)Math.Ceiling(((NbÉlémentsPage - NbColonnes * NbLignes)
                                    / (double)NbLignes));
                    TailleÉlément = tailleHorizontale - (BORDURE_X - BORDURE_X / NbColonnes);
                }
                else
                    TailleÉlément = tailleVerticale - BORDURE_Y;
            }
        }

        /// <summary>
        /// Cette méthode permet de créer l'image (avec une sorte d'indice de page) d'un dossier
        /// puis l'enregistre en PNG de sorte à la réutiliser lorsque voulue.
        /// </summary>
        public void CréerImageDossier()
        {
            // Création du bitmap
            Bitmap temp = new Bitmap(Screen.PrimaryScreen.WorkingArea.Width,
                                     Screen.PrimaryScreen.WorkingArea.Height);

            int posX = ((Screen.PrimaryScreen.WorkingArea.Width - (NbColonnes
                * TailleÉlément) - (NbColonnes - 1) * BORDURE_X) / 2);
            int posY = ((Screen.PrimaryScreen.WorkingArea.Height - (NbLignes
                * TailleÉlément) - (NbLignes - 1) * BORDURE_Y) / 2);

            string texteÉlément = "";

            // On dessine les vignettes
            Image objÉlément = null;

            using (Graphics gTemp = Graphics.FromImage(temp))
            {
                gTemp.SmoothingMode = SmoothingMode.AntiAlias;
                gTemp.FillRectangle(new SolidBrush(Form.DefaultBackColor),
                                    new Rectangle(0, 0, Screen.PrimaryScreen.WorkingArea.Width,
                                                        Screen.PrimaryScreen.WorkingArea.Height));

                for (int i = 0; i < NbLignes; ++i)
                {
                    for (int j = 0; j < NbColonnes
                        && IndiceÉlément < NbÉlémentsPage
                        * (IndicePage + 1); ++j)
                    {
                        gTemp.DrawRectangle(Pens.Black,
                            new Rectangle(posX - 1, posY - 1, TailleÉlément + 1,
                                                              TailleÉlément + 1));
                        // Si c'est un dossier...
                        if (IndiceÉlément < SousDossiers.Count)
                        {
                            SousDossiers[IndiceDossier].Position = new Point(posX, posY);
                            gTemp.FillRectangle(Brushes.Chocolate,
                                new Rectangle(posX - 1, posY - 1, TailleÉlément + 1,
                                                                  TailleÉlément + 1));

                            if (SousDossiers[IndiceDossier].Images.Count != 0)
                                objÉlément = Image.FromFile(SousDossiers[IndiceDossier].Images[0].Chemin);

                            texteÉlément = SousDossiers[IndiceDossier].NomDossier;
                            ++IndiceDossier;
                        }
                        // Sinon, c'est une image
                        else
                        {
                            objÉlément = Image.FromFile(Images[IndiceImage].Chemin);
                            Images[IndiceImage].Position = new Point(posX, posY);
                            gTemp.FillRectangle(Brushes.Silver,
                                new Rectangle(posX - 1, posY - 1, TailleÉlément + 1,
                                                                   TailleÉlément + 1));
                            texteÉlément =
                                Images[IndiceImage].InfosAffichées;
                            ++IndiceImage;
                        }

                        if (objÉlément != null)
                        {
                            int largeur = objÉlément.Width;
                            int hauteur = objÉlément.Height;
                            if (objÉlément.Width > objÉlément.Height
                                && objÉlément.Width > TailleÉlément)
                            {
                                largeur = TailleÉlément;
                                hauteur = objÉlément.Height * TailleÉlément
                                                            / objÉlément.Width;
                            }
                            else if (objÉlément.Height > TailleÉlément)
                            {
                                largeur = objÉlément.Width * TailleÉlément
                                                           / objÉlément.Height;
                                hauteur = TailleÉlément;
                            }
                            // On dessine la vignette concrètement.
                            gTemp.DrawRectangle(Pens.Black,
                                new Rectangle((int)(posX + (TailleÉlément - largeur) / 2) - 1,
                                (int)(posY + (TailleÉlément - hauteur) / 2) - 1,
                                largeur + 1, hauteur + 1));
                            gTemp.DrawImage(objÉlément,
                                new Rectangle((int)(posX + (TailleÉlément - largeur) / 2),
                                (int)(posY + (TailleÉlément - hauteur) / 2),
                                largeur, hauteur));
                        }
                        // On coupe le texte si ce dernier est plus long que la taille d'un élément
                        while (gTemp.MeasureString(texteÉlément, m_policeBase).Width
                               > TailleÉlément)
                            texteÉlément = texteÉlément.Substring(0, texteÉlément.Length - 1);

                        gTemp.DrawString(texteÉlément, m_policeBase, Brushes.Black,
                            posX + TailleÉlément / 2
                            - gTemp.MeasureString(texteÉlément, m_policeBase).Width / 2,
                            posY + TailleÉlément + BORDURE_INFOS);

                        ++IndiceÉlément;
                        posX += TailleÉlément + BORDURE_X;
                    }
                    posX = ((Screen.PrimaryScreen.WorkingArea.Width - (NbColonnes
                        * TailleÉlément) - (NbColonnes - 1)
                        * BORDURE_X) / 2);
                    posY += TailleÉlément + BORDURE_Y;
                }
                // Si on ne se trouve pas dans le dossier racine, on dessine une flèche en haut qui permet
                // de remonter d'un niveau.
                if (SousDossier)
                {
                    int posFlècheHaut = Screen.PrimaryScreen.WorkingArea.Width / 2 - 200;
                    Point[] flècheSuppérieure = {
                                new Point(posFlècheHaut, 37), new Point(posFlècheHaut, 62),
                                new Point(posFlècheHaut + 200, 37), new Point(posFlècheHaut + 400, 62),
                                new Point(posFlècheHaut + 400, 37), new Point(posFlècheHaut + 200, 12)};
                    gTemp.DrawPolygon(Pens.Black, flècheSuppérieure);
                    gTemp.FillPolygon(Brushes.Chocolate, flècheSuppérieure);
                }
                if (!DernierePage())
                {
                    int posFlècheDroite = Screen.PrimaryScreen.WorkingArea.Height / 2 - 200;
                    Point[] flècheSuppérieure = {
                          new Point(Screen.PrimaryScreen.WorkingArea.Width - 62, posFlècheDroite),
                          new Point(Screen.PrimaryScreen.WorkingArea.Width - 37, posFlècheDroite),
                          new Point(Screen.PrimaryScreen.WorkingArea.Width - 12,posFlècheDroite + 200),
                          new Point(Screen.PrimaryScreen.WorkingArea.Width - 37, posFlècheDroite + 400),
                          new Point(Screen.PrimaryScreen.WorkingArea.Width - 62, posFlècheDroite + 400),
                          new Point(Screen.PrimaryScreen.WorkingArea.Width - 37, posFlècheDroite+ 200)};
                    gTemp.DrawPolygon(Pens.Black, flècheSuppérieure);
                    gTemp.FillPolygon(Brushes.Chocolate, flècheSuppérieure);
                }
                if (IndicePage > 0)
                {
                    int posFlècheGauche = Screen.PrimaryScreen.WorkingArea.Height / 2 - 200;
                    Point[] flècheSuppérieure = { new Point(62, posFlècheGauche),
                                               new Point(37, posFlècheGauche),
                                               new Point(12,posFlècheGauche + 200),
                                               new Point(37, posFlècheGauche + 400),
                                               new Point(62, posFlècheGauche + 400),
                                               new Point(37, posFlècheGauche+ 200)};
                    gTemp.DrawPolygon(Pens.Black, flècheSuppérieure);
                    gTemp.FillPolygon(Brushes.Chocolate, flècheSuppérieure);
                }
            }

            // On sauvegarde l'image temporaire.
            temp.Save(@"C:\tempImagePro\" + NomDossier + '_'
                                          + IndicePage + ".png");
            objÉlément.Dispose();
            temp.Dispose();
        }

        public bool DernierePage() { return ((NbPages > 0) ? IndicePage == NbPages - 1 : true); }

        public string Chemin { get; set; }
        public string NomDossier { get; set; }
        public ClDossier Parent { get; set; }
        public bool SousDossier { get; set; }
        public List<ClImage> Images { get; set; }
        public List<ClDossier> SousDossiers { get; set; }
        public int NbPages { get; set; }
        public int IndicePage { get; set; }
        public int NbÉléments { get; set; }
        public int IndiceImage { get; set; }
        public int IndiceÉlément { get; set; }
        public int NbÉlémentsPage { get; set; }
        public int IndiceDossier { get; set; }
        public Point Position { get; set; }
        public int NbColonnes { get; set; }
        public int NbLignes { get; set; }
        public int TailleÉlément { get; set; }

        Font m_policeBase = new Font("Arial", 12);
    }
}