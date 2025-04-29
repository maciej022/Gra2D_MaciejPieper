using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Input;

namespace Gra2D  // Zmienione z GraWPF na Gra2D
{
    public partial class MainWindow : Window
    {
        const int RozmiarSegmentu = 32;
        const int LAKA = 1, LAS = 2, SKALA = 3, DIAMENT = 4, ILE_TERENOW = 5;

        string wybranyRozmiar = "srednia";
        string trudnosc = "normalna";
        int szerokoscMapy = 20;
        int wysokoscMapy = 20;

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
                EtykietaStatus.Content = "Wczytano mapę.";
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

                mapa = new int[wysokoscMapy, szerokoscMapy];
                Random rand = new Random();

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
                EtykietaStatus.Content = "Wygenerowano losową mapę.";
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
            EtykietaDiament.Content = $"Diamenty: {iloscDiamentow}";
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            int nowyX = pozycjaGraczaX;
            int nowyY = pozycjaGraczaY;

            if (e.Key == Key.Up) nowyY--;
            else if (e.Key == Key.Down) nowyY++;
            else if (e.Key == Key.Left) nowyX--;
            else if (e.Key == Key.Right) nowyX++;

            if (nowyX < 0 || nowyX >= szerokoscMapy || nowyY < 0 || nowyY >= wysokoscMapy)
                return;

            int pole = mapa[nowyY, nowyX];

            if (pole == SKALA && !mozePrzechodzicPrzezSkaly)
                return;

            pozycjaGraczaX = nowyX;
            pozycjaGraczaY = nowyY;
            AktualizujPozycjeGracza();

            if (pole == LAS)
            {
                mapa[nowyY, nowyX] = LAKA;
                tablicaTerenu[nowyY, nowyX].Source = obrazyTerenu[LAKA];
                iloscDrewna++;
            }

            if (pole == DIAMENT)
            {
                mapa[nowyY, nowyX] = LAKA;
                tablicaTerenu[nowyY, nowyX].Source = obrazyTerenu[LAKA];
                iloscDiamentow++;
            }

            if (iloscDiamentow >= 3)
                mozePrzechodzicPrzezSkaly = true;

            AktualizujEtykiety();
        }

        private void WczytajMape_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBoxResult result = MessageBox.Show("Czy chcesz wczytać mapę z pliku?", "Wczytaj mapę", MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes)
                {
                    string folder = AppDomain.CurrentDomain.BaseDirectory;
                    string plik = Path.Combine(folder, $"{wybranyRozmiar}_{trudnosc}.txt");

                    if (File.Exists(plik))
                        WczytajMape(plik);
                    else
                        EtykietaStatus.Content = $"Nie znaleziono mapy: {plik}";
                }
                else if (result == MessageBoxResult.No)
                {
                    GenerujLosowaMape();
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

        // Dodane brakujące metody
        private void WybierzRozmiar_Click(object sender, RoutedEventArgs e)
        {
            // Implementacja wyboru rozmiaru
        }

        private void WybierzTrudnosc_Click(object sender, RoutedEventArgs e)
        {
            // Implementacja wyboru trudności
        }

        private void PokazSterowanie_Click(object sender, RoutedEventArgs e)
        {
            // Implementacja pokazywania sterowania
        }

        private void PokazJakGrac_Click(object sender, RoutedEventArgs e)
        {
            // Implementacja pokazywania instrukcji
        }
    }
}