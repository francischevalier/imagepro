using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Printing;

namespace ImagePro
{
    class ClRapportImpression : UtilStJ.Rapport
    {
        // le nb "d'info image" qui rentre dans une page
        const int NB_INFO_IMAGE_PAR_PAGE = 10;

        Font m_police;

        // Le dossier contenant les images � imprimer
        ClDossier m_dossierImprimer;

        public ClRapportImpression()  // Constructeur utile seulement si on veut changer des options de base
        {
             //PageSettings.Landscape = true;  // Pour changer les options par d�faut du rapport
        }

        public void SetDossierImprimer(ClDossier p_dossierImprimer)
        {
            m_dossierImprimer = p_dossierImprimer;
        }

        // Les deux fonctions obligatoires
        protected override int CalculerNbPages()
        {
            int nbPages = (int)Math.Ceiling((double)m_dossierImprimer.Images.Count / NB_INFO_IMAGE_PAR_PAGE); 

            return nbPages;
        }

        protected override void DoPrintPage(System.Drawing.Printing.PrintPageEventArgs p_arg, int p_noPage)
        {
            Graphics g = p_arg.Graphics;
            PageSettings ps = PageSettings; // Ou p_arg.PageSettings (qui est identique)

            int posX = 100;
            int posY = 100;

            int indiceImage = (p_noPage - 1) * NB_INFO_IMAGE_PAR_PAGE;

            // L'indice de la derni�re image � dessin�e dans cette page
            int indiceDerni�reImagePage;


            // Si la derni�re page, on imprime le reste
            if (p_noPage == NbPages)
            {
                indiceDerni�reImagePage = (m_dossierImprimer.Images.Count - ((p_noPage - 1)
                                     * NB_INFO_IMAGE_PAR_PAGE)) + (NB_INFO_IMAGE_PAR_PAGE * (p_noPage-1));
            }
            else
                indiceDerni�reImagePage = NB_INFO_IMAGE_PAR_PAGE * p_noPage;

            while (indiceImage != indiceDerni�reImagePage)
            {
                g.DrawString(String.Format("Nom : {0}\nInfo : {1}",
                        m_dossierImprimer.Images[indiceImage].NomFichier,
                        m_dossierImprimer.Images[indiceImage].TaillesImage),
                        m_police, Brushes.Black, posX, posY);

                posY += 100;
                ++indiceImage;
            }
        }


        // Fonctions facultatives (selon besoin, ici on y cr�e des ressources utiles)
        protected override void DoBeginPrint(System.Drawing.Printing.PrintEventArgs p_arg)
        {
            // Si on ne l'a pas fait ailleurs...
            m_police = new Font("Arial", 0.2F, GraphicsUnit.Inch);
        }

        protected override void DoEndPrint(System.Drawing.Printing.PrintEventArgs p_arg)
        {
            // Si on alloue dans DoBegin, on lib�re ici...
            /// m_police.Dispose();    TEMPORAIREMENT ENLEV� � CAUSE D'UN BOGUE QUELQUE PART
        }

        protected override void AjusterPageSetupDialog(System.Windows.Forms.PageSetupDialog p_psd)
        {
            p_psd.AllowMargins = true;
            p_psd.AllowOrientation = true; // Si on veut s'en occuper...
        }

        protected override void AjusterPrintDialog(System.Windows.Forms.PrintDialog p_pd)
        {
            // p_pd.AllowSomePages = true;  // Est par d�faut...
            // p_pd.PrintToFile = false;    // Est par d�faut
            // p_pd.AllowCurrentPage = true; // Il faudrait que cette notion soit possible dans notre
                                             //  logiciel...
            // p_pd.Document. ...  // Donne acc�s � d'autres options...
        }
    }
}
