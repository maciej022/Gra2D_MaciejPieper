using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using Microsoft.Win32;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        const int RozmiarSegmentu = 32;
        const int LAKA = 1, LAS = 2, SKALA = 3, DIAMENT = 4, ILE_TERENOW = 5;

        string wybranyRozmiar = null;
        string trudnosc = "normalna";
        int szerokoscMapy = 20;
        int wysokoscMapy = 20;
        int wymaganeDiamenty = 5;
        int currentLevel = 1;
        bool isPaused = false;

        int[,] mapa;
        Image[,] tablicaTerenu;
        Image obrazGracza;
        int pozycjaGraczaX = 0;
        int pozycjaGraczaY = 0;
        int iloscDrewna = 0;
        int iloscDiamentow = 0;
        bool mozePrzechodzicPrzezSkaly = false;
        BitmapImage[] obrazyTerenu = new BitmapImage[ILE_TERENOW];

        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;

            WczytajObrazyTerenu();
            obrazGracza = new Image
            {
                Width = RozmiarSegmentu,
                Height = RozmiarSegmentu,
                Source = new BitmapImage(new Uri("gracz.png", UriKind.Relative))
            };
        }

        private void WczytajObrazyTerenu()
        {
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("laka.png", UriKind.Relative));
            obrazyTerenu[LAS] = new BitmapImage(new Uri("las.png", UriKind.Relative));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("skala.png", UriKind.Relative));
            obrazyTerenu[DIAMENT] = new BitmapImage(new Uri("diament.png", UriKind.Relative));
        }

        private void WczytajMape(string sciezka)
        {
            try
            {
                string[] linie = File.ReadAllLines(sciezka);
                wysokoscMapy = linie.Length;
                szerokoscMapy = linie[0].Split(' ').Length;

                mapa = new int[wysokoscMapy, szerokoscMapy];

                for (int y = 0; y < wysokoscMapy; y++)
                {
                    string[] pola = linie[y].Split(' ');
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        mapa[y, x] = int.Parse(pola[x]);
                    }
                }

                GenerujMape();
                EtykietaStatus.Content = $"Wczytano mapę poziomu {currentLevel}.";
            }
            catch (Exception ex)
            {
                EtykietaStatus.Content = "Błąd przy wczytywaniu mapy: " + ex.Message;
            }
        }

        private void GenerujMape()
        {
            SiatkaMapy.Children.Clear();
            SiatkaMapy.RowDefinitions.Clear();
            SiatkaMapy.ColumnDefinitions.Clear();

            SiatkaMapy.HorizontalAlignment = HorizontalAlignment.Center;
            SiatkaMapy.VerticalAlignment = VerticalAlignment.Center;

            for (int y = 0; y < wysokoscMapy; y++)
                SiatkaMapy.RowDefinitions.Add(new RowDefinition { Height = new GridLength(RozmiarSegmentu) });

            for (int x = 0; x < szerokoscMapy; x++)
                SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(RozmiarSegmentu) });

            tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy];

            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    Image obraz = new Image
                    {
                        Width = RozmiarSegmentu,
                        Height = RozmiarSegmentu,
                        Source = obrazyTerenu[mapa[y, x]]
                    };

                    Grid.SetRow(obraz, y);
                    Grid.SetColumn(obraz, x);
                    SiatkaMapy.Children.Add(obraz);
                    tablicaTerenu[y, x] = obraz;
                }
            }

            SiatkaMapy.Children.Add(obrazGracza);
            Panel.SetZIndex(obrazGracza, 1);
            pozycjaGraczaX = 0;
            pozycjaGraczaY = 0;
            AktualizujPozycjeGracza();

            iloscDrewna = 0;
            iloscDiamentow = 0;
            mozePrzechodzicPrzezSkaly = false;
            AktualizujEtykiety();
        }

        private void GenerujLosowaMape()
        {
            try
            {
                if (wybranyRozmiar == null)
                {
                    EtykietaStatus.Content = "Najpierw wybierz rozmiar mapy!";
                    return;
                }

                switch (wybranyRozmiar)
                {
                    case "mala":
                        szerokoscMapy = 10;
                        wysokoscMapy = 10;
                        break;
                    case "srednia":
                        szerokoscMapy = 20;
                        wysokoscMapy = 20;
                        break;
                    case "duza":
                        szerokoscMapy = 30;
                        wysokoscMapy = 30;
                        break;
                }

               
                int difficultyMultiplier = currentLevel;

                switch (trudnosc)
                {
                    case "latwa":
                        wymaganeDiamenty = 3 * difficultyMultiplier;
                        break;
                    case "normalna":
                        wymaganeDiamenty = 5 * difficultyMultiplier;
                        break;
                    case "trudna":
                        wymaganeDiamenty = 8 * difficultyMultiplier;
                        break;
                }

                mapa = new int[wysokoscMapy, szerokoscMapy];
                Random rand = new Random();

                int rockChance = 15 + (5 * currentLevel);
                if (rockChance > 40) rockChance = 40;

                for (int y = 0; y < wysokoscMapy; y++)
                {
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        int los = rand.Next(100);
                        if (los < 50) mapa[y, x] = LAKA;
                        else if (los < 80) mapa[y, x] = LAS;
                        else if (los < 95) mapa[y, x] = SKALA;
                        else mapa[y, x] = DIAMENT;
                    }
                }

                mapa[0, 0] = LAKA;
                GenerujMape();
                EtykietaStatus.Content = $"Wygenerowano losową mapę poziomu {currentLevel}. Zbierz {wymaganeDiamenty} diamentów, aby przejść dalej!";
            }
            catch (Exception ex)
            {
                EtykietaStatus.Content = "Błąd generowania mapy: " + ex.Message;
            }
        }

        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(obrazGracza, pozycjaGraczaY);
            Grid.SetColumn(obrazGracza, pozycjaGraczaX);
        }

        private void AktualizujEtykiety()
        {
            EtykietaDrewno.Content = $"Drewno: {iloscDrewna}";
            EtykietaDiament.Content = $"Diamenty: {iloscDiamentow}/{wymaganeDiamenty} (Poziom {currentLevel})";

          
            if (iloscDiamentow >= wymaganeDiamenty)
            {
                currentLevel++;
                if (currentLevel > 2) 
                {
                    EtykietaStatus.Content = "Gratulacje! Ukończyłeś wszystkie poziomy!";
                    MessageBox.Show("Gratulacje! Ukończyłeś wszystkie poziomy gry!", "Wygrana");
                    Application.Current.Shutdown();
                }
                else
                {
                    EtykietaStatus.Content = $"Gratulacje! Przechodzisz do poziomu {currentLevel}!";
                    MessageBox.Show($"Gratulacje! Przechodzisz do poziomu {currentLevel}!", "Nowy poziom");
                    GenerujLosowaMape();
                }
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (isPaused && e.Key != Key.P)
                return;

            switch (e.Key)
            {
                case Key.P:
                    TogglePause();
                    return;
                case Key.R:
                    RestartGame();
                    return;
                case Key.C:
                    CollectItems();
                    return;
            }

            int nowyX = pozycjaGraczaX;
            int nowyY = pozycjaGraczaY;

            if (e.Key == Key.Up) nowyY--;
            else if (e.Key == Key.Down) nowyY++;
            else if (e.Key == Key.Left) nowyX--;
            else if (e.Key == Key.Right) nowyX++;
            else return;

            if (nowyX < 0 || nowyX >= szerokoscMapy || nowyY < 0 || nowyY >= wysokoscMapy)
                return;

            int pole = mapa[nowyY, nowyX];

            if (pole == SKALA && !mozePrzechodzicPrzezSkaly)
                return;

            pozycjaGraczaX = nowyX;
            pozycjaGraczaY = nowyY;
            AktualizujPozycjeGracza();

            AktualizujEtykiety();
        }

        private void CollectItems()
        {
            int pole = mapa[pozycjaGraczaY, pozycjaGraczaX];

            if (pole == LAS)
            {
                mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                iloscDrewna++;
            }

            if (pole == DIAMENT)
            {
                mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                iloscDiamentow++;
            }

            if (iloscDiamentow >= 3)
                mozePrzechodzicPrzezSkaly = true;

            AktualizujEtykiety();
        }

        private void TogglePause()
        {
            isPaused = !isPaused;
            EtykietaStatus.Content = isPaused ? "Gra zatrzymana" : "Gra wznowiona";
        }

        private void RestartGame()
        {
            currentLevel = 1;
            GenerujLosowaMape();
        }

        private void WczytajMape_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Pliki tekstowe (*.txt)|*.txt|Wszystkie pliki (*.*)|*.*";
                if (openFileDialog.ShowDialog() == true)
                {
                    WczytajMape(openFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                EtykietaStatus.Content = "Błąd podczas wczytywania mapy: " + ex.Message;
            }
        }

        private void GenerujLosowaMape_Click(object sender, RoutedEventArgs e)
        {
            GenerujLosowaMape();
        }

        private void WybierzRozmiar_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window()
            {
                Title = "Wybierz rozmiar mapy",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var stackPanel = new StackPanel();

            var malaButton = new Button() { Content = "Mała (10x10)", Margin = new Thickness(10) };
            malaButton.Click += (s, args) => { wybranyRozmiar = "mala"; dialog.Close(); };

            var sredniaButton = new Button() { Content = "Średnia (20x20)", Margin = new Thickness(10) };
            sredniaButton.Click += (s, args) => { wybranyRozmiar = "srednia"; dialog.Close(); };

            var duzaButton = new Button() { Content = "Duża (30x30)", Margin = new Thickness(10) };
            duzaButton.Click += (s, args) => { wybranyRozmiar = "duza"; dialog.Close(); };

            stackPanel.Children.Add(malaButton);
            stackPanel.Children.Add(sredniaButton);
            stackPanel.Children.Add(duzaButton);

            dialog.Content = stackPanel;
            dialog.ShowDialog();
        }

        private void WybierzTrudnosc_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window()
            {
                Title = "Wybierz trudność",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var stackPanel = new StackPanel();

            var latwaButton = new Button() { Content = "Łatwa", Margin = new Thickness(10) };
            latwaButton.Click += (s, args) => { trudnosc = "latwa"; dialog.Close(); };

            var normalnaButton = new Button() { Content = "Normalna", Margin = new Thickness(10) };
            normalnaButton.Click += (s, args) => { trudnosc = "normalna"; dialog.Close(); };

            var trudnaButton = new Button() { Content = "Trudna", Margin = new Thickness(10) };
            trudnaButton.Click += (s, args) => { trudnosc = "trudna"; dialog.Close(); };

            stackPanel.Children.Add(latwaButton);
            stackPanel.Children.Add(normalnaButton);
            stackPanel.Children.Add(trudnaButton);

            dialog.Content = stackPanel;
            dialog.ShowDialog();
        }

        private void PokazSterowanie_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Sterowanie:\n" +
                          "Strzałki - poruszanie się\n" +
                          "C - zbieranie przedmiotów\n" +
                          "P - pauza\n" +
                          "R - restart poziomu\n\n" +
                          "Zbieraj drewno i diamenty poruszając się po mapie.\n" +
                          "Po zebraniu 3 diamentów możesz przechodzić przez skały.", "Sterowanie");
        }

        private void PokazJakGrac_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Cel gry:\n" +
                          "- Zbierz wymaganą liczbę diamentów (zależy od trudności i poziomu)\n" +
                          "- Zbierz drewno, aby zwiększyć swój wynik\n" +
                          "- Po zebraniu 3 diamentów możesz przechodzić przez skały\n" +
                          "- Każdy kolejny poziom jest trudniejszy\n\n" +
                          $"Poziom 1: {wymaganeDiamenty} diamentów wymagane\n" +
                          $"Poziom 2: {wymaganeDiamenty * 2} diamentów wymagane", "Jak grać");
        }
    }
}