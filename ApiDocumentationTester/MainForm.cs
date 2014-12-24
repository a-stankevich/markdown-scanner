﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OneDrive.ApiDocumentation.Validation;
using OneDrive.ApiDocumentation.Validation.Json;
using OneDrive.ApiDocumentation.Validation.Http;
using OneDrive.ApiDocumentation.Validation.Param;

namespace OneDrive.ApiDocumentation.Windows
{
    public partial class MainForm : Form
    {
        string m_AccessToken = null;

        DocSet CurrentDocSet { get; set; }
        ValidationError[] DocSetLoadErrors { get; set; }

        internal string AccessToken { get { return m_AccessToken; } }
        
        BindingList<RequestParameters> m_Parameters = new BindingList<RequestParameters>();

        public MainForm()
        {
            InitializeComponent();

            textBoxBaseURL.Text = Properties.Settings.Default.ApiBaseRoot;
            textBoxClientId.Text = Properties.Settings.Default.ClientId;
            textBoxAuthScopes.Text = Properties.Settings.Default.AuthScopes;
            textBoxMethodRequestParameterFile.Text = Properties.Settings.Default.RequestParametersFile;
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog { ShowNewFolderButton = false };
            var result = dialog.ShowDialog();
            if (DialogResult.OK == result)
            {
                Properties.Settings.Default.LastOpenedPath = dialog.SelectedPath;
                Properties.Settings.Default.Save();
                OpenFolder(dialog.SelectedPath);
            }
        }

        private void OpenFolder(string path)
        {
            CurrentDocSet = new DocSet(path);

            listBoxDocuments.DisplayMember = "DisplayName";
            listBoxDocuments.DataSource = CurrentDocSet.Files;

            ValidationError[] loadErrors;
            if (!CurrentDocSet.ScanDocumentation(out loadErrors))
            {
                DocSetLoadErrors = loadErrors;
            }

            CurrentDocSet.TryReadRequestParameters(textBoxMethodRequestParameterFile.Text);

            listBoxResources.DisplayMember = "ResourceType";
            listBoxResources.DataSource = CurrentDocSet.Resources;

            methodsPage.LoadFromDocSet(CurrentDocSet);

            m_Parameters = new BindingList<RequestParameters>(CurrentDocSet.RequestParameters);

            listBoxParameters.DisplayMember = "Method";
            listBoxParameters.DataSource = m_Parameters;

            LoadSelectedDocumentPreview();
        }

        private void listBoxDocuments_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadSelectedDocumentPreview();
        }

        private void LoadSelectedDocumentPreview()
        {
            var doc = listBoxDocuments.SelectedItem as DocFile;
            if (null != doc && !string.IsNullOrEmpty(doc.HtmlContent) && null != webBrowserPreview)
            {
                webBrowserPreview.DocumentText = doc.HtmlContent;
            }
        }

        private void LoadDocumentIntoEditor(DocFile docFile)
        {
            //docFileEditorControl1.LoadFile(docFile);
        }

        private void openLastFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var lastPath = Properties.Settings.Default.LastOpenedPath;
            if (!string.IsNullOrEmpty(lastPath))
            {
                OpenFolder(lastPath);
            }
        }

        private void listBoxResources_SelectedIndexChanged(object sender, EventArgs e)
        {
            var resource = ((ListBox)sender).SelectedItem as ResourceDefinition;
            textBoxResourceData.Text = resource.JsonExample;
        }

     

        private async void signInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await SignInAsync();
        }

        public async Task SignInAsync()
        {
            var clientId = textBoxClientId.Text;
            var scopes = textBoxAuthScopes.Text.Split(',');

            m_AccessToken = await MicrosoftAccountSDK.Windows.FormMicrosoftAccountAuth.GetAuthenticationToken(clientId, scopes, MicrosoftAccountSDK.Windows.OAuthFlow.ImplicitGrant, this);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void validateJsonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SchemaValidatorForm form = new SchemaValidatorForm { ResourceCollection = CurrentDocSet.ResourceCollection };
            form.Show();
        }

        private void textBoxClientId_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ClientId = textBoxClientId.Text;
            Properties.Settings.Default.Save();
        }

        private void textBoxAuthScopes_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AuthScopes = textBoxAuthScopes.Text;
            Properties.Settings.Default.Save();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ValidationError[] errors;
            if (!CurrentDocSet.ValidateLinks(checkBoxLinkWarnings.Checked, out errors))
            {
                textBoxLinkValidation.Text = (from m in errors select string.Format("{0}: {1}", m.Source, m.Message)).ComponentsJoinedByString(Environment.NewLine);
            }
            else
            {
                textBoxLinkValidation.Text = "No link errors detected";
            }
        }

        private void textBoxMethodRequestParameterFile_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.RequestParametersFile = textBoxMethodRequestParameterFile.Text;
            Properties.Settings.Default.Save();
        }


        private RequestParameters SelectedRequestConfiguration { get { return listBoxParameters.SelectedItem as RequestParameters; } }
        private void listBoxParameters_SelectedIndexChanged(object sender, EventArgs e)
        {
            methodParametersEditorControl1.OpenRequestParameters(SelectedRequestConfiguration, CurrentDocSet);
        }

        private void buttonAddParameters_Click(object sender, EventArgs e)
        {
            RequestParameters newParams = new RequestParameters { Note = "New request configuration." };
            m_Parameters.Add(newParams);
            listBoxParameters.SelectedItem = newParams;
        }

        private void buttonDeleteParameters_Click(object sender, EventArgs e)
        {
            var itemToRemove = listBoxParameters.SelectedItem as RequestParameters;
            m_Parameters.Remove(itemToRemove);
        }

        private void buttonSaveParameterFile_Click(object sender, EventArgs e)
        {
            // Write out m_Parameters to disk somewhere
            CurrentDocSet.TryWriteRequestParameters(textBoxMethodRequestParameterFile.Text, m_Parameters.ToArray());
        }

      

        private void showLoadErrorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (DocSetLoadErrors != null)
            {
                ErrorDisplayForm.ShowErrorDialog(DocSetLoadErrors, this);
            }
            else
            {
                MessageBox.Show("No errors detected during load.");
            }
        }
    }
}