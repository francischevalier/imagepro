using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Threading;

namespace ImagePro
{
    public partial class ImagePro : Form
    {
        static string m_cheminRacine;
        ClDossier m_dossierCourant;
        ClRapportImpression m_impression = new ClRapportImpression();

        // Marge entre chaque image en pixels.
        const int BORDURE_CONTOUR = 75;
        const int BORDURE_X = 10;
        const int BORDURE_Y = 30;
        const int BORDURE_INFOS = 5;

        Font m_policeBase = new Font("Arial", 12);

        public ImagePro(string[] args)
        {
            if (args.Length != 0 && Directory.Exists(args[0]))
                m_cheminRacine = args[0];
            else
                m_cheminRacine = Directory.GetCurrentDirectory();

            m_dossierCourant = new ClDossier(m_cheminRacine, null);

            // On supprime le répertoire d'images temporaire au cas où il resterait
            // des vieilles images.
            if (!Directory.Exists(@"C:\tempImagePro\"))
                Directory.CreateDirectory(@"C:\tempImagePro\");
            else
            {
                Directory.Delete(@"C:\tempImagePro\", true);
                Directory.CreateDirectory(@"C:\tempImagePro\");
            }
            
            InitializeComponent();

            // Normallement, s'il n'y a pas d'élément à afficher tout court, on affiche un message
            if (m_dossierCourant.NbÉléments > 0)
                m_dossierCourant.FaireMiseEnPage();
        }

        private void ImagePro_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // pour l'anti-aliasing
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (File.Exists(@"C:\tempImagePro\" + m_dossierCourant.NomDossier + '_'
                                                + m_dossierCourant.IndicePage + ".png"))
            {
                g.DrawImage(Image.FromFile(@"C:\tempImagePro\" + m_dossierCourant.NomDossier + '_'
                                           + m_dossierCourant.IndicePage + ".png"), new Point(0, 0));
            }
            else
            {
                m_dossierCourant.CréerImageDossier();
                Invalidate();
            }
        }

        /// <summary>
        /// Pour dessiner une image en taille maximale selon la grandeur de l'image
        /// et de l'espace disponible dans l'écran.
        /// </summary>
        /// <param name="p_indice"></param>
        void DessinerImageMaximisée(int p_indice)
        {
            using (Graphics g = CreateGraphics())
            {
                g.FillRectangle(Brushes.LightGray, new Rectangle(0, 0,
                                Screen.PrimaryScreen.WorkingArea.Width,
                                Screen.PrimaryScreen.WorkingArea.Height));
                int largeur = m_dossierCourant.Images[p_indice].TailleEnPixels.X;
                int hauteur = m_dossierCourant.Images[p_indice].TailleEnPixels.Y;

                if (largeur > Screen.PrimaryScreen.WorkingArea.Width)
                {
                    largeur = Screen.PrimaryScreen.WorkingArea.Width;
                    hauteur = largeur * hauteur / m_dossierCourant.Images[p_indice].TailleEnPixels.X;
                }

                if (hauteur > Screen.PrimaryScreen.WorkingArea.Height)
                {
                    int ancienneHauteur = hauteur;

                    hauteur = Screen.PrimaryScreen.WorkingArea.Height-20;
                    largeur = hauteur * largeur / ancienneHauteur;
                }

                int posX = (Screen.PrimaryScreen.WorkingArea.Width - largeur) / 2;
                int posY = (Screen.PrimaryScreen.WorkingArea.Height - 20 - hauteur) / 2;
                g.DrawImage(Image.FromFile(m_dossierCourant.Images[p_indice].Chemin),
                             new Rectangle(posX, posY, largeur, hauteur));
            }
        }

        // Pour savoir si une image est maximisé.
        bool m_imageEstMaximisé = false;

        /// <summary>
        /// On vérifie si la souris se situe vis-à-vis une vignette et s'il
        /// s'agit d'un clic droit ou gauche. Si c'est un clic gauche sur une vignette,
        /// on change l'information affichée sous l'image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImagePro_MouseClick(object sender, MouseEventArgs e)
        {
            if ( ! m_imageEstMaximisé && e.Button != MouseButtons.Right)
            {
                // On crée un rectangle qui va contenir la portion de chaque vignette à tour de rôle
                // afin de vérifier si la position de la souris s'y situe.
                Rectangle recVignette = new Rectangle();
                recVignette.Size =
                               new Size(m_dossierCourant.TailleÉlément, m_dossierCourant.TailleÉlément);
                // On crée un "rectangle" qui va contenir la portion occupée par la souris
                Rectangle recSouris = new Rectangle(new Point(e.X, e.Y), new Size(1, 1));
                // On arrête de chercher si on a trouvé quelle image a été cliquée.
                bool aÉtéCliquée = false;
                int indiceImage = -1;
                for (int i = m_dossierCourant.NbÉlémentsPage * m_dossierCourant.IndicePage;
                    ! aÉtéCliquée && i != m_dossierCourant.Images.Count; ++i)
                {
                    recVignette.Location = m_dossierCourant.Images[i].Position;
                    aÉtéCliquée = recVignette.IntersectsWith(recSouris);

                    if (aÉtéCliquée)
                        indiceImage = i;
                }

                using (Graphics g = CreateGraphics())
                {
                    if (aÉtéCliquée)
                    {
                        SizeF taille = g.MeasureString(m_dossierCourant.Images[indiceImage].InfosAffichées,
                                                        m_policeBase);
                        if (m_dossierCourant.Images[indiceImage].NomAffiché)
                        {
                            m_dossierCourant.Images[indiceImage].NomAffiché = false;
                            m_dossierCourant.Images[indiceImage].InfosAffichées =
                                                m_dossierCourant.Images[indiceImage].TaillesImage;
                        }
                        else
                        {
                            m_dossierCourant.Images[indiceImage].NomAffiché = true;
                            m_dossierCourant.Images[indiceImage].InfosAffichées =
                                              m_dossierCourant.Images[indiceImage].NomFichier;
                        }

                        g.FillRectangle(new SolidBrush(BackColor),
                                m_dossierCourant.Images[indiceImage].Position.X,
                                m_dossierCourant.Images[indiceImage].Position.Y
                                                  + m_dossierCourant.TailleÉlément + BORDURE_INFOS,
                                m_dossierCourant.TailleÉlément,
                                (int)taille.Height);

                        // On coupe le texte si ce dernier dépasse la taille d'un élément.
                        string texteÉlément = m_dossierCourant.Images[indiceImage].InfosAffichées;
                        while (g.MeasureString(texteÉlément, m_policeBase).Width
                                > m_dossierCourant.TailleÉlément)
                            texteÉlément = texteÉlément.Substring(0, texteÉlément.Length - 1);
                        
                        g.DrawString(texteÉlément, m_policeBase, Brushes.Black,
                            m_dossierCourant.Images[indiceImage].Position.X
                                                               + m_dossierCourant.TailleÉlément / 2
                            - g.MeasureString(texteÉlément, m_policeBase).Width / 2,
                            m_dossierCourant.Images[indiceImage].Position.Y
                                                 + m_dossierCourant.TailleÉlément + BORDURE_INFOS);
                    }
                }

                // Si on double clique sur la flèche suppérieure
                if (m_dossierCourant.SousDossier)
                {
                    int posFlècheHaut = Screen.PrimaryScreen.WorkingArea.Width / 2 - 200;
                    Rectangle recFlèche = new Rectangle(posFlècheHaut, 12, 400, 50);
                    recFlèche.Location = new Point(posFlècheHaut, 12);
                    if (recFlèche.IntersectsWith(recSouris))
                    {
                        m_dossierCourant = m_dossierCourant.Parent;
                        m_dossierCourant.FaireMiseEnPage();
                        Refresh();
                    }
                }
                // Si on double clique sur la flèche droite
                if (!m_dossierCourant.DernierePage())
                {
                    int posFlècheDroite = Screen.PrimaryScreen.WorkingArea.Height / 2 - 200;
                    Rectangle recFlèche = new Rectangle(Screen.PrimaryScreen.WorkingArea.Width - 62,
                                                         posFlècheDroite, 50, 400);
                    recFlèche.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - 62,
                                                    posFlècheDroite);
                    if (recFlèche.IntersectsWith(recSouris))
                    {
                        ++m_dossierCourant.IndicePage;
                        m_dossierCourant.FaireMiseEnPage();
                        Refresh();
                    }
                }
                // Si on double clique sur la flèche gauche
                if (m_dossierCourant.IndicePage > 0)
                {
                    int posFlècheGauche = Screen.PrimaryScreen.WorkingArea.Height / 2 - 200;
                    Rectangle recFlèche = new Rectangle(12, posFlècheGauche, 50, 400);
                    recFlèche.Location = new Point(12, posFlècheGauche);
                    if (recFlèche.IntersectsWith(recSouris))
                    {
                        --m_dossierCourant.IndicePage;
                        m_dossierCourant.FaireMiseEnPage();
                        Refresh();
                    }
                }
            }
            else if ( ! m_imageEstMaximisé)
            {
                m_menuContextuel.Show(this, new Point(e.X, e.Y));

                // On envoi à l'objet m_impression le dossier contenant les infos à imprimer
                m_impression.SetDossierImprimer(m_dossierCourant);
            }
            else
            {
                // Un clic droit permet de reculer (toujours pratique)
                if (e.Button == MouseButtons.Right)
                    DessinerImageMaximisée(m_dossierCourant.ObtenirImagePrécédente());
                else
                    DessinerImageMaximisée(m_dossierCourant.ObtenirProchaineImage());
            }
        }

        /// <summary>
        /// Le double clic permet d'ouvrir des dossiers mais également d'afficher
        /// une image en taille maximisée.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImagePro_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Si on double clic avec le bouton droit, on fait rien.
            if (!m_imageEstMaximisé && e.Button != MouseButtons.Right)
            {
                // Pour le cas où l'on double-clique sur un dossier
                Rectangle recÉlément = new Rectangle();
                recÉlément.Size = new Size(m_dossierCourant.TailleÉlément,
                                            m_dossierCourant.TailleÉlément);
                Rectangle recSouris = new Rectangle(new Point(e.X, e.Y), new Size(1, 1));
                bool aÉtéCliquée = false;

                for (int i = 0; !aÉtéCliquée && i != m_dossierCourant.SousDossiers.Count; ++i)
                {
                    recÉlément.Location = m_dossierCourant.SousDossiers[i].Position;
                    aÉtéCliquée = recÉlément.IntersectsWith(recSouris);

                    if (aÉtéCliquée)
                    {
                        m_dossierCourant = m_dossierCourant.SousDossiers[i];
                        m_dossierCourant.FaireMiseEnPage();
                        Refresh();
                    }
                }

                // Si on double clique sur une image
                for (int i = m_dossierCourant.NbÉlémentsPage * m_dossierCourant.IndicePage;
                    ! aÉtéCliquée && i != m_dossierCourant.Images.Count; ++i)
                {
                    recÉlément.Location = m_dossierCourant.Images[i].Position;
                    aÉtéCliquée = recÉlément.IntersectsWith(recSouris);

                    if (aÉtéCliquée)
                    {
                        m_imageEstMaximisé = true;
                        m_dossierCourant.IndiceImage = i;
                        DessinerImageMaximisée(i);
                    }
                }
            }
            else if (e.Button != MouseButtons.Right)
            {
                using (Graphics g = CreateGraphics())
                {
                    g.DrawImage(Image.FromFile(@"C:\tempImagePro\" + m_dossierCourant.NomDossier
                                   + '_' + m_dossierCourant.IndicePage + ".png"), new Point(0, 0));
                }

                m_imageEstMaximisé = false;
            }
        }

        System.Windows.Forms.ContextMenu m_menuContextuel;

        /// <summary>
        /// Pour le menu contextuel lors d'un clic droit dans le dossier courant.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImagePro_Load(object sender, EventArgs e)
        {
            m_menuContextuel = new System.Windows.Forms.ContextMenu();
            System.Windows.Forms.MenuItem menuItemImprimer;
            menuItemImprimer = new System.Windows.Forms.MenuItem();
            System.Windows.Forms.MenuItem menuItemApperçu;
            menuItemApperçu = new System.Windows.Forms.MenuItem();

            m_menuContextuel.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] { menuItemImprimer, menuItemApperçu });
            menuItemImprimer.Index = 0;
            menuItemImprimer.Text = "Imprimer";
            menuItemApperçu.Index = 1;
            menuItemApperçu.Text = "Apperçu";

            menuItemImprimer.Click += new System.EventHandler(this.Imprimer);
            menuItemApperçu.Click += new System.EventHandler(this.AfficherAperçu);
        }

        public void Imprimer(object sender, System.EventArgs e)
        {
            m_impression.Imprimer();
        }

        public void AfficherAperçu(object sender, System.EventArgs e)
        {
            m_impression.AfficherApercu();
        }
    }
}