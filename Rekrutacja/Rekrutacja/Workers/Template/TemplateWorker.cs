using Soneta.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Soneta.Kadry;
using Soneta.KadryPlace;
using Soneta.Types;
using Rekrutacja.Workers.Template;
using System.Collections;
using System.Windows.Forms;

//Rejetracja Workera - Pierwszy TypeOf określa jakiego typu ma być wyświetlany Worker, Drugi parametr wskazuje na jakim Typie obiektów będzie wyświetlany Worker
[assembly: Worker(typeof(TemplateWorker), typeof(Pracownicy))]
namespace Rekrutacja.Workers.Template
{
    public class TemplateWorker
    {
        List<Pracownik> workers = new List<Pracownik>();
        //Aby parametry działały prawidłowo dziedziczymy po klasie ContextBase
        public class TemplateWorkerParametry : ContextBase
        {
            private char _defaultChar;

            char[] name = new char [] { '+', '*', '-', '/' };
            [Caption("A")]
            public double X { get; set; }

            [Caption("B")]
            public double Y { get; set; }

            [Caption("Data obliczeń")]
            public Date DataObliczen { get; set; }

            [Caption("Operacja")]
            public char Operacja
            {
                get { return _defaultChar; }
                set
                {
                    if (name.Contains(value))
                    {
                        _defaultChar = value;
                    }
                    else
                    {
                        string message = "Wybierz jeden z dozwolonych znaków '+', '-', '*', '/'";
                        string caption = "Błąd operacji";
                        MessageBox.Show(message, caption);
                        //throw new Exception("Wpisz jeden ze znaków '+','-','*','/'");
                    }
                }
            }
            public TemplateWorkerParametry(Context context) : base(context)
            {
                this.X = 0;
                this.Y = 0;
                this.Operacja = '+';
                this.DataObliczen = Date.Today;
            }
        }
        /*Metoda, która miala za zadanie zwracać wynik operacji jednak nie wiem czemu nawet przez użycie jej bez zmiennych lub 
         * zwracanie wyniku na sztywno powodowało zatrzymanie aplikacji i rzucenie błędem*/
        //public double MakeOperation(doble x, double y , char op)
        //{
        //    //double result = op == '+' ? x + y : op == '-' ? x - y : op == '*' ? x * y : x / y;
        //    return 6.0;
        //}


        //Obiekt Context jest to pudełko które przechowuje Typy danych, aktualnie załadowane w aplikacji
        //Atrybut Context pobiera z "Contextu" obiekty które aktualnie widzimy na ekranie
        [Context]
        public Context Cx { get; set; }
        //Pobieramy z Contextu parametry, jeżeli nie ma w Context Parametrów mechanizm sam utworzy nowy obiekt oraz wyświetli jego formatkę
        [Context]
        public TemplateWorkerParametry Parametry { get; set; }
        //Atrybut Action - Wywołuje nam metodę która znajduje się poniżej
        [Action("Kalkulator",
           Description = "Prosty kalkulator ",
           Priority = 10,
           Mode = ActionMode.ReadOnlySession,
           Icon = ActionIcon.Accept,
           Target = ActionTarget.ToolbarWithText)]


        public void WykonajAkcje()
        {
            //Włączenie Debug, aby działał należy wygenerować DLL w trybie DEBUG
            DebuggerSession.MarkLineAsBreakPoint();
            //Pobieranie danych z Contextu
            Pracownik pracownik = null;
            if (Cx.Contains(typeof(Pracownik)))
            {
                pracownik = (Pracownik)Cx[typeof(Pracownik)];
            }
            if (Cx.Contains(typeof(Pracownik[])))
            {
                var someObject = Cx[typeof(Pracownik[])];
                IList workersColletion = (IList)someObject;
                foreach (var worker in workersColletion)
                {
                    workers.Add((Pracownik)worker);
                    pracownik = (Pracownik)worker;
                }
            }
            double result = this.Parametry.Operacja == '+' ? this.Parametry.X + this.Parametry.Y :
                this.Parametry.Operacja == '-' ? this.Parametry.X - this.Parametry.Y :
                this.Parametry.Operacja == '*' ? this.Parametry.X * this.Parametry.Y :
                this.Parametry.Y == 0 ? this.Parametry.X / (this.Parametry.Y = 1) :
                this.Parametry.X / this.Parametry.Y;
            //Modyfikacja danych
            //Aby modyfikować dane musimy mieć otwartą sesję, któa nie jest read only
            using (Session nowaSesja = this.Cx.Login.CreateSession(false, false, "ModyfikacjaPracownika"))
            {
                //Otwieramy Transaction aby można było edytować obiekt z sesji
                using (ITransaction trans = nowaSesja.Logout(true))
                {
                    foreach (var worker in workers)
                    {
                        //Pobieramy obiekt z Nowo utworzonej sesji
                        var pracownikZSesja = nowaSesja.Get(worker);
                        //Features - są to pola rozszerzające obiekty w bazie danych, dzięki czemu nie jestesmy ogarniczeni to kolumn jakie zostały utworzone przez producenta
                        pracownikZSesja.Features["DataObliczen"] = this.Parametry.DataObliczen;
                        //pracownikZSesja.Features["Wynik"] = this.MakeOperation(this.Parametry.X, this.Parametry.Y, this.Parametry.Operacja);
                        pracownikZSesja.Features["Wynik"] = result;
                        //Zatwierdzamy zmiany wykonane w sesji
                        trans.CommitUI();
                    }
                }
                //Zapisujemy zmiany
                nowaSesja.Save();

                // Zakładam że zgodnie ze biblioteka napiana jest w standardzie C# 8.0 i instrukcja using zapewnia poprawne wywolanie IDisposable
            }

        }
    }
}