namespace Cambios
{
    using Modelos;
    using Servicos;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public partial class Form1 : Form
    {
        #region Atributos

        private List<Rate> Rates;

        private NetworkService networkService;

        private ApiService apiService;

        private DialogService dialogService;

        private DataService dataService;

        #endregion


        public Form1()
        {
            InitializeComponent();
            networkService = new NetworkService();
            apiService = new ApiService();
            dialogService = new DialogService();
            dataService = new DataService();
            LoadRates();
        }

        private async void LoadRates()
        {
            bool load;

            lblResultado.Text = "A atualizar taxas...";

            var connection = networkService.CheckConnection();

            if (!connection.IsSuccess)
            {
                LoadLocalRates();
                load = false;
            }
            else
            {
                await LoadApiRates();
                load = true;
            }

            if (Rates.Count == 0)
            {
                lblResultado.Text = "Não há ligação á Internet" + Environment.NewLine +
                    "e não foram previamente carrregadas as taxas." + Environment.NewLine +
                    "Tente mais tarde!";

                lblStatus.Text = "Primeira inicialização deverá ter ligação á Internet";
            }

            comboBoxOrigem.DataSource = Rates;
            comboBoxOrigem.DisplayMember = "Name";

            //Corrige bug da microsoft
            comboBoxDestino.BindingContext = new BindingContext();

            comboBoxDestino.DataSource = Rates;
            comboBoxDestino.DisplayMember = "Name";

            lblResultado.Text = "Taxas atualizadas...";

            if (load)
            {
                lblStatus.Text = string.Format("Taxas carregadas da internet em {0:F}", DateTime.Now);
            }
            else
            {
                lblStatus.Text = string.Format("Taxas carregadas da Base de Dados.");
            }

            progressBar1.Value = 100;

            btnConveter.Enabled = true;
            btnTroca.Enabled = true;
        }


        private void LoadLocalRates()
        {
            Rates = dataService.GetData();
        }

        private async Task LoadApiRates()
        {
            progressBar1.Value = 0;

            var response = await apiService.GetRates("http://cambios.somee.com", "/api/Rates");

            Rates = (List<Rate>)response.Result;

            dataService.DeleteData();

            dataService.SaveData(Rates);
        }

        private void btnConveter_Click(object sender, EventArgs e)
        {
            Converter();
        }

        private void Converter()
        {
            if (string.IsNullOrEmpty(txtValor.Text))
            {
                dialogService.ShowMessage("Erro", "Insira um valor a converter");
                return;
            }

            decimal valor;
            if (!decimal.TryParse(txtValor.Text, out valor))
            {
                dialogService.ShowMessage("Erro de conversão", "Valor terá de ser numérico");
                return;
            }

            if (comboBoxOrigem.SelectedItem == null)
            {
                dialogService.ShowMessage("Erro", "Tem que escolher uma moeda a converter");
                return;
            }

            if (comboBoxDestino.SelectedItem == null)
            {
                dialogService.ShowMessage("Erro", "Tem que escolher uma moeda de destino para converter");
                return;
            }

            var taxaOrigem = (Rate)comboBoxOrigem.SelectedItem;
            var taxaDestino = (Rate)comboBoxDestino.SelectedItem;

            var valorConvetido = valor / (decimal)taxaOrigem.TaxRate * (decimal)taxaDestino.TaxRate;

            lblResultado.Text = string.Format("{0} {1:C2} = {2} {3:C2}",
                taxaOrigem.Code,
                valor,
                taxaDestino.Code,
                valorConvetido);
        }

        private void btnTroca_Click(object sender, EventArgs e)
        {
            Troca();
        }

        private void Troca()
        {
            var aux = comboBoxOrigem.SelectedItem;
            comboBoxOrigem.SelectedItem = comboBoxDestino.SelectedItem;
            comboBoxDestino.SelectedItem = aux;
            Converter();
        }
    }
}
