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

        // Le dossier contenant les images à imprimer
        ClDossier m_dossierImprimer;

        public ClRapportImpression()  // Constructeur utile seulement si on veut changer des options de base
        {
             //PageSettings.Landscape = true;  // Pour changer les options par défaut du rapport
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

            // L'indice de la dernière image à dessinée dans cette page
            int indiceDernièreImagePage;


            // Si la dernière page, on imprime le reste
            if (p_noPage == NbPages)
            {
                indiceDernièreImagePage = (m_dossierImprimer.Images.Count - ((p_noPage - 1)
                                     * NB_INFO_IMAGE_PAR_PAGE)) + (NB_INFO_IMAGE_PAR_PAGE * (p_noPage-1));
            }
            else
                indiceDernièreImagePage = NB_INFO_IMAGE_PAR_PAGE * p_noPage;

            while (indiceImage != indiceDernièreImagePage)
            {
                g.DrawString(String.Format("Nom : {0}\nInfo : {1}",
                        m_dossierImprimer.Images[indiceImage].NomFichier,
                        m_dossierImprimer.Images[indiceImage].TaillesImage),
                        m_police, Brushes.Black, posX, posY);

                posY += 100;
                ++indiceImage;
            }
        }


        // Fonctions facultatives (selon besoin, ici on y crée des ressources utiles)
        protected override void DoBeginPrint(System.Drawing.Printing.PrintEventArgs p_arg)
        {
            // Si on ne l'a pas fait ailleurs...
            m_police = new Font("Arial", 0.2F, GraphicsUnit.Inch);
        }

        protected override void DoEndPrint(System.Drawing.Printing.PrintEventArgs p_arg)
        {
            // Si on alloue dans DoBegin, on libère ici...
            /// m_police.Dispose();    TEMPORAIREMENT ENLEVÉ À CAUSE D'UN BOGUE QUELQUE PART
        }

        protected override void AjusterPageSetupDialog(System.Windows.Forms.PageSetupDialog p_psd)
        {
            p_psd.AllowMargins = true;
            p_psd.AllowOrientation = true; // Si on veut s'en occuper...
        }

        protected override void AjusterPrintDialog(System.Windows.Forms.PrintDialog p_pd)
        {
            // p_pd.AllowSomePages = true;  // Est par défaut...
            // p_pd.PrintToFile = false;    // Est par défaut
            // p_pd.AllowCurrentPage = true; // Il faudrait que cette notion soit possible dans notre
                                             //  logiciel...
            // p_pd.Document. ...  // Donne accès à d'autres options...
        }
    }
}
