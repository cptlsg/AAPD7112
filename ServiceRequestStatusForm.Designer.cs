namespace MunicipalServicesApp
{
    partial class ServiceRequestStatusForm
    {
        private System.ComponentModel.IContainer? components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Form-level settings only. The controls are created in BuildLayout
        /// (ServiceRequestStatusForm.cs) because the tabs, lists and tree are
        /// filled from the data structures at run time.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();
            //
            // ServiceRequestStatusForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1360, 800);
            this.MinimumSize = new System.Drawing.Size(1180, 700);
            this.Name = "ServiceRequestStatusForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Municipal Services Application - Service Request Status";
            this.ResumeLayout(false);
        }
    }
}
