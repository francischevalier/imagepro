using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;


namespace UtilStJ
{
    /// <summary>
    /// Summary description for ClRapport.
    /// </summary>
    public abstract class Rapport : PrintDocument
    {
        /// <summary>
        /// Créer le rapport.
        /// </summary>
        protected Rapport()
        {
            PrinterSettings = m_pageSettings.PrinterSettings;
            PrintController = new StandardPrintController(); // Pour enlever la boîte Annuler,
                                                             //  (elle reste dans Aperçu...)
            QueryPageSettings += new QueryPageSettingsEventHandler(this.MonQPSEH);
        }

        // ******************************************************************************
        // ************************** INTERFACE DE LA CLASSE ****************************
        // ******************************************************************************

        /// <summary>
        /// « Configurer » permet de faire afficher un PageSetupDialog. Par défaut, on
        /// ne peut qu'y choisir une imprimante. Pour activer le choix de marges,
        /// d'orientation ou de papier, il faut que la classe dérivée redéfinisse la
        /// fonction AjusterPageSetupDialog et y ajoute ce qu'il faut. Si on les active, il
        /// faut écrire le code nécessaire pour s'en occuper aussi (c'est pourquoi ils
        /// ne sont pas activés par défaut). Attention, les ajustements de l'imprimante
        /// peuvent changer l'orientation du papier et d'autres paramètres (on peut
        /// quand même ne pas s'en occuper...).
        /// </summary>
        public void Configurer()
        {
            PageSetupDialog psDlg = new PageSetupDialog();
            psDlg.PageSettings = m_pageSettings;
            psDlg.PrinterSettings = m_pageSettings.PrinterSettings;

            psDlg.AllowMargins = false;
            psDlg.AllowOrientation = false;
            psDlg.AllowPaper = false;
            psDlg.AllowPrinter = true; // Est aussi true par défaut...

            AjusterPageSetupDialog(psDlg);
            // BOGUE ! Il transforme mal les unités des marges !!!! 
            psDlg.EnableMetric = true; // Même avec ça, nouveau en 2.0, c'est bogué, car précision insuffisante (à tester prochaine version !)
			psDlg.ShowDialog();
        }

        PrintPreviewDialog m_ppDlg;

        /// <summary>
        /// « AfficherApercu » permet de prévisualiser, mais aussi d'appeler l'impression
        /// par le bouton présenté. Ce serait normalement l'impression directe, mais la
        /// classe l'intercepte pour présenter plutôt la boîte de choix des pages (etc.),
        /// comme si on appelait Imprimer.
        /// </summary>
        public void AfficherApercu()
        {
            m_typeImpression = TypeImpression.DEMANDE_APERÇU;

            m_nbPages = CalculerNbPages();
            m_noPageDepart = 1;
            m_noPageCourante = 1;
            m_noPageLimite = m_nbPages; // L'aperçu montre tout
            m_nbCopies = 1;
            m_noCopie = 1;

            PrintPreviewDialog ppDlg = new PrintPreviewDialog();
            m_ppDlg = ppDlg;
            ppDlg.Document = this;

            if (m_formeParent != null)
            {
                ppDlg.Size = m_formeParent.Size;
                ppDlg.StartPosition = FormStartPosition.Manual;
                ppDlg.DesktopLocation = m_formeParent.DesktopLocation;
            }

            ppDlg.ShowDialog();
        }

        /// <summary>
        /// « ImprimerTout » envoie toutes les pages à l'impression, sur l'imprimante par
        /// défaut, etc., sans confirmation. On ne devrait normalement pas l'utiliser !
        /// </summary>
        public void ImprimerTout()
        {
            m_typeImpression = TypeImpression.DEMANDE_IMPRIMERTOUT;

            m_nbPages = CalculerNbPages();
            m_noPageCourante = 1;
            m_noPageDepart = 1;
            m_noCopie = 1;
            m_nbCopies = 1;
            m_noPageLimite = m_nbPages;

            Print();
        }

        /// <summary>
        /// « Imprimer » devrait être la fonction normale utilisée pour l'impression. Elle
        /// présente le choix de pages (ce qui permet de changer d'imprimante et certains
        /// de ses paramètres). On peut aussi choisir le nombre de copies, l'assemblage,
        /// etc., mais ces opérations sont gérées automatiquement (par la classe). Le
        /// choix d'imprimer dans un fichier n'est pas géré et est désactivé. Idem pour
        /// l'impression d'une sélection. On peut toujours redéfinir AjusterPrintDialog
        /// pour modifier ce qui est possible.
        /// </summary>
        public void Imprimer()
        {
            m_typeImpression = TypeImpression.DEMANDE_CHOIXIMPRESSION;
            m_nbPages = CalculerNbPages();

            if (ChoisirImpression())
                ImprimerSelonChoix();
        }

        /// <summary>
        /// « NbPages » renvoie le nombre de pages qui a été déterminé par la fonction
        /// CalculerNbPages. On l'utilisera normalement pendant les DoPrintPage si on
        /// veut écrire des choses du genre de « Page 1 de 8 ».
        /// </summary>
        public int NbPages
        {
            get { return m_nbPages; }
        }

        /// <summary>
        /// Renvoie le PageSettings courant.
        /// </summary>
        public PageSettings PageSettings
        {
            get { return m_pageSettings; }
        }

        /// <summary>
        /// « Associer » permet d'associer la forme à la classe de rapport pour que la
        /// fenêtre de prévisualisation soit ajustée à la même taille et position (si
        /// possible). On devrait l'appeler dans le constructeur de la forme principale
        /// de notre application et lui passer this.
        /// </summary>
        public void Associer(Form p_parent)
        {
            m_formeParent = p_parent;
        }

        // Fonctions pour ajustement des marges physiques de l'imprimante. En aperçu,
        // renvoie 0,0,largeurTotal,hauteurTotale. Tout est en centièmes de pouces.
        /// <summary>
        /// Renvoie la position gauche minimum
        /// </summary>
        public float MinX
        {
            get { return m_corrections.X; }
        }

        /// <summary>
        /// Renvoie la position du haut minimum
        /// </summary>
        public float MinY
        {
            get { return m_corrections.Y; }
        }

        /// <summary>
        /// Renvoie la position droite maximum
        /// </summary>
        public float MaxX
        {
            get { return m_corrections.Right; }
        }

        /// <summary>
        /// Renvoie la position du bas maximum
        /// </summary>
        public float MaxY
        {
            get { return m_corrections.Bottom; }
        }

        /// <summary>
        /// Indique si on dessine l'aperçu (valide dans DoPrintPage et DoBeginPrint seulement)
        /// </summary>
        public bool EnAperçu
        {
            get 
            { 
                return m_typeImpression == TypeImpression.DEMANDE_APERÇU
                       || m_typeImpression == TypeImpression.PRÉPARATION_APERÇU_TERMINÉE; 
            }
        }

        // ******************************************************************************
        // ******************** FONCTIONS À REDÉFINIR ABSOLUMENT ************************
        // ******************************************************************************

        /// <summary>
        /// « CalculerNbPages » doit renvoyer le nombre exact de pages que le rapport
        /// aura. Elle est appelée avant l'impression ou la prévisualisation.
        /// </summary>
        /// <returns>
        /// </returns>
        protected abstract int CalculerNbPages();

        /// <summary>
        /// « DoPrintPage » doit s'occuper d'imprimer la page demandée (elle ne doit pas
        /// toucher à HasMorePages, la classe s'en occupe).
        /// </summary>
        protected abstract void DoPrintPage(PrintPageEventArgs p_arg, int p_noPage);

        // ******************************************************************************
        // **************** FONCTIONS À REDÉFINIR SEULEMENT SI UTILES *******************
        // ******************************************************************************

        //************************** Ressources d'impression ****************************
        /// <summary>
        /// -- Allocation ou ouverture d'items pour impression, en particulier, on peut y
        ///    allouer les polices, etc. La version de base ne fait rien.
        /// </summary>
        protected virtual void DoBeginPrint(PrintEventArgs p_arg)
        { }

        /// <summary>
        /// -- Libérations ou fermeture (rarement utile). La version de base ne fait rien.
        /// </summary>
        protected virtual void DoEndPrint(PrintEventArgs p_arg)
        { }

        // ************************ Ajustements des paramètres **************************
        /// <summary>
        /// -- « AjusterPageSetupDialog » est appelée par Configurer et permet de modifier
        ///    AllowMargins, AllowOrientation, AllowPaper, AllowPrinter, MinMargins,
        ///    ShowHelp (et HelpRequest) ainsi que ShowNetwork. On peut aussi modifier des
        ///    éléments des PageSettings et PrinterSettings (propriétés du PageSetupDialog
        ///    reçu) pour changer les valeurs proposées dans les éléments modifiables. La
        ///    version de base de cette fonction ne fait rien.
        /// </summary>
        protected virtual void AjusterPageSetupDialog(PageSetupDialog p_psd)
        { }

        /// <summary>
        /// -- « AjusterPrintDialog » est appelée par Imprimer (ou par le bouton
        ///    d'impression de la prévisualisation) et permet de modifier
        ///    AllowPrintToFile (et PrintToFile), AllowSelection, AllowSomePages, ShowHelp
        ///    (et HelpRequest) ainsi que ShowNetwork. On peut aussi modifier des éléments
        ///    du PrinterSetting (propriété du PrintDialog reçu) pour changer les valeurs
        ///    proposées dans certains éléments modifiables. La version de base de cette
        ///    fonction ne fait rien.
        /// </summary>
        protected virtual void AjusterPrintDialog(PrintDialog p_pd)
        { }

        // ******************************************************************************
        // ************************* IMPLÉMENTATION DE LA CLASSE ************************
        // ******************************************************************************

        // Type pour savoir ce qu'on est en train de faire :-)
        enum TypeImpression { DEMANDE_APERÇU, PRÉPARATION_APERÇU_TERMINÉE, DEMANDE_IMPRESSION_DANS_APERÇU, 
                              DEMANDE_IMPRIMERTOUT, DEMANDE_CHOIXIMPRESSION }

        // Données
        Form m_formeParent = null;
        TypeImpression m_typeImpression;
        int m_nbPages;
        int m_noPageCourante;
        int m_noPageDepart;
        int m_noPageLimite;
        int m_noCopie;
        int m_nbCopies;
        RectangleF m_corrections; // Était Rectangle (et les Min/Max était int)
        PageSettings m_pageSettings = new PageSettings();  // On le garde en mémoire d'une fois à l'autre
                                                           //  (peut être ajusté par AjusterPageSettings)
        bool ChoisirImpression()  // Renvoie false si annulé
        {
            // Un nouveau PrinterSettings est nécessaire, il ne semble pas y avoir moyen de modifier
            // ses paramètres correctement (en particulier, le bouton radio du choix de pages).
            PrinterSettings ps = new PrinterSettings();
            ps.PrinterName = m_pageSettings.PrinterSettings.PrinterName;
            m_pageSettings.PrinterSettings = ps;
            PrinterSettings = m_pageSettings.PrinterSettings;

            ps.MinimumPage = 1;
            ps.MaximumPage = m_nbPages;
            ps.FromPage = ps.MinimumPage;
            ps.ToPage = ps.MaximumPage;

            PrintDialog d = new PrintDialog();
            d.Document = this;
            d.PrinterSettings = ps;
            d.AllowSomePages = true;
            d.AllowPrintToFile = false;
            d.AllowSelection = false;
            d.UseEXDialog = true; // Nouveau .NET 2, mais semble essentiel sous Vista 64 (à tout le moins)

            AjusterPrintDialog(d);
            return DialogResult.OK == d.ShowDialog();
        }

        void ImprimerSelonChoix()
        {
            PrinterSettings ps = m_pageSettings.PrinterSettings;
            m_noPageDepart = m_noPageCourante = ps.FromPage;
            m_noPageLimite = ps.ToPage;
            m_nbCopies = ps.Copies;
            m_noCopie = 1;
            Print();
        }

        // Les fonctions suivantes devraient être privées, mais on ne peut pas
        // réduire les droits d'accès des types gérés .NET
        /// <summary>
        /// Ne pas redéfinir.
        /// </summary>
        protected override void OnBeginPrint(PrintEventArgs p_arg)
        {
            m_corrections.X = -1; // Flag pour le recharger

            switch (m_typeImpression)
            {
                case TypeImpression.DEMANDE_APERÇU :
                    base.OnBeginPrint(p_arg);
                    DoBeginPrint(p_arg);
                    m_typeImpression = TypeImpression.PRÉPARATION_APERÇU_TERMINÉE;
                    break;

                case TypeImpression.PRÉPARATION_APERÇU_TERMINÉE :
                    // base.OnBeginPrint(p_arg);

                    if (ChoisirImpression())
                    {
                        m_typeImpression = TypeImpression.DEMANDE_IMPRESSION_DANS_APERÇU;
                        ImprimerSelonChoix();
                        m_ppDlg.Close();
                    }

                    p_arg.Cancel = true; // On l'a fait nous-mêmes !
                    break;

                case TypeImpression.DEMANDE_IMPRESSION_DANS_APERÇU :
                    break;

                default :
                    // De Imprimer ou ImprimerTout()...
                    base.OnBeginPrint(p_arg);
                    DoBeginPrint(p_arg);
                    break;
            }
        }

        /// <summary>
        /// Ne pas redéfinir.
        /// </summary>
        protected override void OnEndPrint(PrintEventArgs p_arg)
        {
            DoEndPrint(p_arg);
            base.OnEndPrint(p_arg);
        }

        /// <summary>
        /// Ne pas redéfinir.
        /// </summary>
        protected override void OnPrintPage(PrintPageEventArgs p_arg)
        {
            if (m_corrections.X == -1)
            {
                if (EnAperçu)
                    m_corrections = p_arg.PageSettings.PrinterSettings.DefaultPageSettings.Bounds;
                else
                {
                    m_corrections = p_arg.PageSettings.PrinterSettings.DefaultPageSettings.PrintableArea;

                    if (p_arg.PageSettings.Landscape)
                    {
                        m_corrections = new RectangleF(m_corrections.Top, m_corrections.Left,
                                                       m_corrections.Height, m_corrections.Width);
                    }
                }
            }

            p_arg.Graphics.ResetTransform();
            p_arg.Graphics.TranslateTransform(-MinX, -MinY);

            base.OnPrintPage(p_arg); // Bof... fait probablement rien
            DoPrintPage(p_arg, m_noPageCourante);

            if (m_pageSettings.PrinterSettings.Collate)
            {
                if (++m_noPageCourante > m_noPageLimite)
                {
                    m_noPageCourante = m_noPageDepart;
                    ++m_noCopie;
                }

                p_arg.HasMorePages = m_noCopie <= m_nbCopies;
            }
            else
            {
                if (++m_noCopie > m_nbCopies)
                {
                    m_noCopie = 1;
                    ++m_noPageCourante;
                }

                p_arg.HasMorePages = m_noPageCourante <= m_noPageLimite;
            }
        }

        void MonQPSEH(Object sender, QueryPageSettingsEventArgs p_arg)
        {
            p_arg.PageSettings = m_pageSettings; // Il doit y avoir un moyen plus simple...
        }
    }


#if FAUTCORRIGERBOGUE
#if !L_AUTRE_BOGUE_EST_CORRIGÉ
            PPDlg ppDlg = new PPDlg(this, m_nbPages, m_formeParent);
#else
            PrintPreviewDialog ppDlg = new PrintPreviewDialog();
            ppDlg.Document = this;

            if (m_formeParent != null)
            {
                ppDlg.Size = m_formeParent.Size;
                ppDlg.StartPosition = FormStartPosition.Manual;
                ppDlg.DesktopLocation = m_formeParent.DesktopLocation;
            }

            ppDlg.Text = "Aperçu avant impression";
#endif

#if !LE_BOGUE_EST_CORRIGÉ
#else
            if (System.Globalization.RegionInfo.CurrentRegion.IsMetric)
            {
                Margins marge = m_pageSettings.Margins; // Pour simplifier
                marge.Top = (int)Math.Ceiling(marge.Top * 2.54);
                marge.Bottom = (int)Math.Ceiling(marge.Bottom * 2.54);
                marge.Left = (int)Math.Ceiling(marge.Left * 2.54);
                marge.Right = (int)Math.Ceiling(marge.Right * 2.54);
            }

            if (DialogResult.OK != psDlg.ShowDialog())
            {
                // Annulation : remettre comme c'était !
                if (System.Globalization.RegionInfo.CurrentRegion.IsMetric)
                {
                    Margins marge = m_pageSettings.Margins; // Pour simplifier
                    marge.Top = (int)(marge.Top / 2.54);
                    marge.Bottom = (int)(marge.Bottom / 2.54);
                    marge.Left = (int)(marge.Left / 2.54);
                    marge.Right = (int)(marge.Right / 2.54);
                }
            }
#endif

    internal class PPDlg : Form
    {
        public PPDlg(PrintDocument p_pd, int p_nbPages)
            : this(p_pd, p_nbPages, null)
        { }

        public PPDlg(PrintDocument p_pd, int p_nbPages, Form p_parent)
        {
            InitializeComponent();

            m_pd = p_pd;
            m_pageMax = p_nbPages;
            m_parent = p_parent;
            m_pageCourante = 1;

            printPreviewControl.Document = m_pd;

            if (m_parent != null)
            {
                Text = Text + " - " + m_parent.Text;
                Location = m_parent.Location;
                Size = m_parent.Size;
                m_parent.Hide(); // Ou .Visible = false;
                WindowState = m_parent.WindowState;
            }

            MettreÀJourContrôles();
            DialogResult = DialogResult.Cancel;
        }

        PrintDocument m_pd;
        int m_pageMax;
        Form m_parent;
        int m_pageCourante;


        private void MettreÀJourContrôles()
        {
            int nbParPage = printPreviewControl.Columns * printPreviewControl.Rows;
            printPreviewControl.StartPage = m_pageCourante - 1;

            toolStripStatusLabelNoPage.Text =
                String.Format((nbParPage == 1) ? "Page {0} de {2}" : "Page {0} à {1} de {2}",
                              m_pageCourante, m_pageCourante + nbParPage - 1, m_pageMax);

            toolStripStatusLabelZoom.Text =
                String.Format("Zoom {0} %", (int)(100.0 * printPreviewControl.Zoom));

            toolStripButtonPagePrécédente.Enabled = (m_pageCourante > nbParPage);
            toolStripButtonPageSuivante.Enabled = (m_pageCourante + nbParPage <= m_pageMax);

            toolStripButtonAfficherUnePage.Enabled = (nbParPage != 1);
            toolStripButtonAfficher2x2Pages.Enabled = (nbParPage != 4);
        }

        private void toolStripButtonPagePrécédente_Click(object sender, EventArgs e)
        {
            m_pageCourante -= printPreviewControl.Columns * printPreviewControl.Rows;
            MettreÀJourContrôles();
        }

        private void toolStripButtonPageSuivante_Click(object sender, EventArgs e)
        {
            m_pageCourante += printPreviewControl.Columns * printPreviewControl.Rows;
            MettreÀJourContrôles();
        }

        private void toolStripButtonAfficherUnePage_Click(object sender, EventArgs e)
        {
            printPreviewControl.Columns = 1;
            printPreviewControl.Rows = 1;
            MettreÀJourContrôles();
        }

        private void toolStripButtonAfficher2x2Pages_Click(object sender, EventArgs e)
        {
            printPreviewControl.Columns = 2;
            printPreviewControl.Rows = 2;
            MettreÀJourContrôles();
        }

        private void PPDlg_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_parent != null)
                m_parent.Show(); // ou .Visible = true;
        }

        private void Zoom(double p_facteur)
        {
            double zoom = printPreviewControl.Zoom * p_facteur;

            if (zoom < 0.25)
                zoom = 0.25;
            else
                if (zoom > 16)
                    zoom = 16;

            printPreviewControl.Zoom = zoom;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (e.Delta < 0)
                Zoom(1.25);
            else
                Zoom(1.0 / 1.25);

            MettreÀJourContrôles();
        }

        private void toolStripMenuItem025_Click(object sender, EventArgs e)
        {
            printPreviewControl.Zoom = 0.25;
            MettreÀJourContrôles();
        }

        private void toolStripMenuItem05_Click(object sender, EventArgs e)
        {
            printPreviewControl.Zoom = 0.5;
            MettreÀJourContrôles();
        }

        private void toolStripMenuItem100_Click(object sender, EventArgs e)
        {
            printPreviewControl.Zoom = 1.0;
            MettreÀJourContrôles();
        }

        private void toolStripMenuItem200_Click(object sender, EventArgs e)
        {
            printPreviewControl.Zoom = 2.0;
            MettreÀJourContrôles();
        }

        private void toolStripMenuItem400_Click(object sender, EventArgs e)
        {
            printPreviewControl.Zoom = 4.0;
            MettreÀJourContrôles();
        }

        private void toolStripButtonImprimer_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void toolStripButtonFermer_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // De .designer.cs

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PPDlg));
            this.printPreviewControl = new System.Windows.Forms.PrintPreviewControl();
            this.toolStripAperçu = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripButtonPagePrécédente = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonPageSuivante = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonAfficherUnePage = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonAfficher2x2Pages = new System.Windows.Forms.ToolStripButton();
            this.toolStripDropDownButtonZoom = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripMenuItem025 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem05 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem100 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem200 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem400 = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabelNoPage = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelZoom = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripButtonImprimer = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonFermer = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripAperçu.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // printPreviewControl
            // 
            this.printPreviewControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.printPreviewControl.Location = new System.Drawing.Point(12, 28);
            this.printPreviewControl.Name = "printPreviewControl";
            this.printPreviewControl.Size = new System.Drawing.Size(550, 354);
            this.printPreviewControl.TabIndex = 0;
            this.printPreviewControl.UseAntiAlias = true;
            // 
            // toolStripAperçu
            // 
            this.toolStripAperçu.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStripAperçu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonImprimer,
            this.toolStripSeparator1,
            this.toolStripLabel1,
            this.toolStripButtonPagePrécédente,
            this.toolStripButtonPageSuivante,
            this.toolStripSeparator2,
            this.toolStripButtonAfficherUnePage,
            this.toolStripButtonAfficher2x2Pages,
            this.toolStripDropDownButtonZoom,
            this.toolStripSeparator3,
            this.toolStripButtonFermer});
            this.toolStripAperçu.Location = new System.Drawing.Point(0, 0);
            this.toolStripAperçu.Name = "toolStripAperçu";
            this.toolStripAperçu.Size = new System.Drawing.Size(574, 25);
            this.toolStripAperçu.TabIndex = 1;
            this.toolStripAperçu.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Text = "Page :";
            // 
            // toolStripButtonPagePrécédente
            // 
            this.toolStripButtonPagePrécédente.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonPagePrécédente.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonPagePrécédente.Name = "toolStripButtonPagePrécédente";
            this.toolStripButtonPagePrécédente.Text = "précédente";
            this.toolStripButtonPagePrécédente.Click += new System.EventHandler(this.toolStripButtonPagePrécédente_Click);
            // 
            // toolStripButtonPageSuivante
            // 
            this.toolStripButtonPageSuivante.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonPageSuivante.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonPageSuivante.Name = "toolStripButtonPageSuivante";
            this.toolStripButtonPageSuivante.Text = "suivante";
            this.toolStripButtonPageSuivante.Click += new System.EventHandler(this.toolStripButtonPageSuivante_Click);
            // 
            // toolStripButtonAfficherUnePage
            // 
            this.toolStripButtonAfficherUnePage.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonAfficherUnePage.Enabled = false;
            this.toolStripButtonAfficherUnePage.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonAfficherUnePage.Name = "toolStripButtonAfficherUnePage";
            this.toolStripButtonAfficherUnePage.Text = "1x1";
            this.toolStripButtonAfficherUnePage.Click += new System.EventHandler(this.toolStripButtonAfficherUnePage_Click);
            // 
            // toolStripButtonAfficher2x2Pages
            // 
            this.toolStripButtonAfficher2x2Pages.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonAfficher2x2Pages.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonAfficher2x2Pages.Name = "toolStripButtonAfficher2x2Pages";
            this.toolStripButtonAfficher2x2Pages.Text = "2x2";
            this.toolStripButtonAfficher2x2Pages.Click += new System.EventHandler(this.toolStripButtonAfficher2x2Pages_Click);
            // 
            // toolStripDropDownButtonZoom
            // 
            this.toolStripDropDownButtonZoom.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripDropDownButtonZoom.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem025,
            this.toolStripMenuItem05,
            this.toolStripMenuItem100,
            this.toolStripMenuItem200,
            this.toolStripMenuItem400});
            // this.toolStripDropDownButtonZoom.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButtonZoom.Image")));
            this.toolStripDropDownButtonZoom.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButtonZoom.Name = "toolStripDropDownButtonZoom";
            this.toolStripDropDownButtonZoom.Text = "Zoom";
            // 
            // toolStripMenuItem025
            // 
            this.toolStripMenuItem025.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripMenuItem025.Name = "toolStripMenuItem025";
            this.toolStripMenuItem025.Text = "25 %";
            this.toolStripMenuItem025.Click += new System.EventHandler(this.toolStripMenuItem025_Click);
            // 
            // toolStripMenuItem05
            // 
            this.toolStripMenuItem05.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripMenuItem05.Name = "toolStripMenuItem05";
            this.toolStripMenuItem05.Text = "50 %";
            this.toolStripMenuItem05.Click += new System.EventHandler(this.toolStripMenuItem05_Click);
            // 
            // toolStripMenuItem100
            // 
            this.toolStripMenuItem100.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripMenuItem100.Name = "toolStripMenuItem100";
            this.toolStripMenuItem100.Text = "100 %";
            this.toolStripMenuItem100.Click += new System.EventHandler(this.toolStripMenuItem100_Click);
            // 
            // toolStripMenuItem200
            // 
            this.toolStripMenuItem200.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripMenuItem200.Name = "toolStripMenuItem200";
            this.toolStripMenuItem200.Text = "200 %";
            this.toolStripMenuItem200.Click += new System.EventHandler(this.toolStripMenuItem200_Click);
            // 
            // toolStripMenuItem400
            // 
            this.toolStripMenuItem400.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripMenuItem400.Name = "toolStripMenuItem400";
            this.toolStripMenuItem400.Text = "400 %";
            this.toolStripMenuItem400.Click += new System.EventHandler(this.toolStripMenuItem400_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabelNoPage,
            this.toolStripStatusLabelZoom});
            this.statusStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Table;
            this.statusStrip.Location = new System.Drawing.Point(0, 382);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(574, 23);
            this.statusStrip.TabIndex = 2;
            this.statusStrip.Text = "statusStrip1";
            // 
            // toolStripStatusLabelNoPage
            // 
            this.toolStripStatusLabelNoPage.AutoSize = false;
            this.toolStripStatusLabelNoPage.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.toolStripStatusLabelNoPage.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripStatusLabelNoPage.Name = "toolStripStatusLabelNoPage";
            this.toolStripStatusLabelNoPage.Size = new System.Drawing.Size(150, 17);
            this.toolStripStatusLabelNoPage.Text = "toolStripStatusLabelNoPage";
            this.toolStripStatusLabelNoPage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // toolStripStatusLabelZoom
            // 
            this.toolStripStatusLabelZoom.AutoSize = false;
            this.toolStripStatusLabelZoom.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.toolStripStatusLabelZoom.Name = "toolStripStatusLabelZoom";
            this.toolStripStatusLabelZoom.Size = new System.Drawing.Size(150, 17);
            this.toolStripStatusLabelZoom.Text = "toolStripStatusLabel1";
            this.toolStripStatusLabelZoom.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // toolStripButtonImprimer
            // 
            this.toolStripButtonImprimer.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            //this.toolStripButtonImprimer.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonImprimer.Image")));
            this.toolStripButtonImprimer.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonImprimer.Name = "toolStripButtonImprimer";
            this.toolStripButtonImprimer.Text = "Imprimer";
            this.toolStripButtonImprimer.Click += new System.EventHandler(this.toolStripButtonImprimer_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            // 
            // toolStripButtonFermer
            // 
            this.toolStripButtonFermer.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            // this.toolStripButtonFermer.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonFermer.Image")));
            this.toolStripButtonFermer.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonFermer.Name = "toolStripButtonFermer";
            this.toolStripButtonFermer.Text = "Fermer";
            this.toolStripButtonFermer.ToolTipText = "Fermer l\'aperçu sans imprimer";
            this.toolStripButtonFermer.Click += new System.EventHandler(this.toolStripButtonFermer_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            // 
            // PPDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(574, 405);
            this.Controls.Add(this.printPreviewControl);
            this.Controls.Add(this.toolStripAperçu);
            this.Controls.Add(this.statusStrip);
            this.Name = "PPDlg";
            this.Text = "Aperçu avant impression";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PPDlg_FormClosing);
            this.toolStripAperçu.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PrintPreviewControl printPreviewControl;
        private System.Windows.Forms.ToolStrip toolStripAperçu;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripButton toolStripButtonPageSuivante;
        private System.Windows.Forms.ToolStripButton toolStripButtonAfficher2x2Pages;
        private System.Windows.Forms.ToolStripButton toolStripButtonAfficherUnePage;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelNoPage;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripButton toolStripButtonPagePrécédente;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButtonZoom;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem025;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem05;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem100;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem200;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem400;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelZoom;
        private System.Windows.Forms.ToolStripButton toolStripButtonImprimer;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripButtonFermer;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
    }
#endif
}
