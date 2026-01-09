using System.Windows;

namespace HazardGuessr
{
    public partial class ConfirmDialog : Window
    {
        public bool Result { get; private set; } = false;

        // メッセージを指定して作成
        public ConfirmDialog(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        // メッセージとタイトルを指定して作成
        public ConfirmDialog(string message, string title) : this(message)
        {
            Title = title;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            this.DialogResult = true;
            this.Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            this.DialogResult = false;
            this.Close();
        }

        // Enterキーで「はい」、Escapeキーで「いいえ」
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == System.Windows.Input.Key.Enter)
            {
                YesButton_Click(this, null);
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                NoButton_Click(this, null);
            }
        }
    }
}