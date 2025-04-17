using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        public const int LAS = 1;
        public const int LAKA = 2;
        public const int SKALA = 3;
        public const int DIAMENT = 4; // Typ terenu: diament
        public const int ILE_TERENOW = 5;

        private int[,] mapa;
        private int szerokoscMapy;
        private int wysokoscMapy;
        private Image[,] tablicaTerenu;
        private const int RozmiarSegmentu = 32;

        private BitmapImage[] obrazyTerenu = new BitmapImage[ILE_TERENOW];
        private int pozycjaGraczaX = 0;
        private int pozycjaGraczaY = 0;
        private Image obrazGracza;
        private int iloscDrewna = 0;
        private int iloscDiamentow = 0; // Licznik diamentów
        private string wybranyRozmiar = "mala";
        private string trudnosc = "latwy";

        public MainWindow()
        {
            InitializeComponent();
            WczytajObrazyTerenu();

            obrazGracza = new Image
            {
                Width = RozmiarSegmentu,
                Height = RozmiarSegmentu
            };

            BitmapImage bmpGracza = new BitmapImage();
            bmpGracza.BeginInit();
            bmpGracza.UriSource = new Uri("gracz.png", UriKind.Relative);
            bmpGracza.EndInit();
            obrazGracza.Source = bmpGracza;

            // Domyślne ustawienia wyboru
            wybranyRozmiar = "mala";
            trudnosc = "latwy";
        }

        private void WczytajObrazyTerenu()
        {
            obrazyTerenu[LAS] = new BitmapImage(new Uri("las.png", UriKind.Relative));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("laka.png", UriKind.Relative));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("skala.png", UriKind.Relative));
            obrazyTerenu[DIAMENT] = new BitmapImage(new Uri("diament.jpg", UriKind.Relative)); // Obrazek diamentu
        }

        private void WczytajMape(string sciezkaPliku)
        {
            try
            {
                var linie = File.ReadAllLines(sciezkaPliku);
                wysokoscMapy = linie.Length;
                szerokoscMapy = linie[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;

                mapa = new int[wysokoscMapy, szerokoscMapy];

                for (int y = 0; y < wysokoscMapy; y++)
                {
                    var czesci = linie[y].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        int teren = int.Parse(czesci[x]);

                        // Zapobiegamy umieszczaniu skał na pozycji (0,0) oraz na jej brzegach
                        if ((x == 0 && y == 0) || (x == 0 && y == 1) || (x == 1 && y == 0) ||
                            (x == szerokoscMapy - 1 && y == 0) || (x == 0 && y == wysokoscMapy - 1) ||
                            (x == szerokoscMapy - 1 && y == wysokoscMapy - 1))
                        {
                            teren = LAKA; // Zamiana skały na łąkę w tych krytycznych miejscach
                        }

                        if (teren == SKALA)
                        {
                            if (x > 0 && y > 0 && x < szerokoscMapy - 1 && y < wysokoscMapy - 1)
                                mapa[y, x] = SKALA;
                            else
                                mapa[y, x] = LAKA;
                        }
                        else
                        {
                            mapa[y, x] = teren;
                        }
                    }
                }

                SiatkaMapy.Children.Clear();
                SiatkaMapy.RowDefinitions.Clear();
                SiatkaMapy.ColumnDefinitions.Clear();

                for (int y = 0; y < wysokoscMapy; y++)
                {
                    SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RozmiarSegmentu) });
                }
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RozmiarSegmentu) });
                }

                tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy];
                for (int y = 0; y < wysokoscMapy; y++)
                {
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        Image obraz = new Image
                        {
                            Width = RozmiarSegmentu,
                            Height = RozmiarSegmentu
                        };

                        int rodzaj = mapa[y, x];
                        if (rodzaj >= 1 && rodzaj < ILE_TERENOW)
                            obraz.Source = obrazyTerenu[rodzaj];
                        else
                            obraz.Source = null;

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
                EtykietaDrewna.Content = "Drewno: " + iloscDrewna;
                EtykietaDiamenty.Content = "Diamenty: " + iloscDiamentow;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wczytywania mapy: " + ex.Message);
            }
        }

        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(obrazGracza, pozycjaGraczaY);
            Grid.SetColumn(obrazGracza, pozycjaGraczaX);
        }

        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            int nowyX = pozycjaGraczaX;
            int nowyY = pozycjaGraczaY;

            if (e.Key == Key.Up) nowyY--;
            else if (e.Key == Key.Down) nowyY++;
            else if (e.Key == Key.Left) nowyX--;
            else if (e.Key == Key.Right) nowyX++;

            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy)
            {
                if (mapa[nowyY, nowyX] != SKALA)
                {
                    pozycjaGraczaX = nowyX;
                    pozycjaGraczaY = nowyY;
                    AktualizujPozycjeGracza();
                }
            }

            if (e.Key == Key.C)
            {
                if (mapa[pozycjaGraczaY, pozycjaGraczaX] == LAS)
                {
                    mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                    tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                    iloscDrewna++;
                    EtykietaDrewna.Content = "Drewno: " + iloscDrewna;
                }

                if (mapa[pozycjaGraczaY, pozycjaGraczaX] == DIAMENT)
                {
                    mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                    tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                    iloscDiamentow++;
                    EtykietaDiamenty.Content = "Diamenty: " + iloscDiamentow;

                    if ((wybranyRozmiar == "mala" && iloscDiamentow >= 1) ||
                        (wybranyRozmiar == "srednia" && iloscDiamentow >= 2) ||
                        (wybranyRozmiar == "duza" && iloscDiamentow >= 3))
                    {
                        MessageBox.Show("Gratulacje! Zebrano wystarczającą liczbę diamentów. Masz teraz umiejętność przechodzenia przez skały!");
                    }
                }
            }
        }

        private void WczytajMape_Click(object sender, RoutedEventArgs e)
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            string plik = Path.Combine(folder, $"{wybranyRozmiar}_{trudnosc}.txt");

            if (File.Exists(plik))
                WczytajMape(plik);
            else
                MessageBox.Show($"Nie znaleziono mapy: {plik}");
        }

        private void WybierzRozmiar_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                wybranyRozmiar = button.Tag.ToString();
            }
        }

        private void WybierzTrudnosc_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                trudnosc = button.Tag.ToString();
            }
        }

        // Metody obsługi rozwijania menu:
        private void PokazTrudnosci_Click(object sender, RoutedEventArgs e)
        {
            // Przełącz widoczność panelu wyboru trudności
            PanelTrudnosci.Visibility = PanelTrudnosci.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void PokazRozmiary_Click(object sender, RoutedEventArgs e)
        {
            // Przełącz widoczność panelu wyboru rozmiarów mapy
            PanelRozmiarow.Visibility = PanelRozmiarow.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}