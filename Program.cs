using System;
using System.Speech.Synthesis;
using System.Windows.Forms;
using System.Threading;

namespace Fala
{
    public class Program
    {
        private static string ultimoTexto = "";
        private static bool running = true;
        private static bool paused = false;
        private static int rate = 5;
        private static string velocidade = "-10%";
        private static SpeechSynthesizer fala;

        [STAThread]
        public static void Main(string[] args)
        {


            // para não começar com o que já está na area de transferência
            ultimoTexto = Clipboard.GetText();

            using (fala = new SpeechSynthesizer())
            {

                if (args.Length > 0)
                {
                    rate = int.Parse(args[0]);
                }

                if (args.Length > 1)
                {
                    velocidade = $@"{int.Parse(args[1])}%";
                }

                fala.Rate = rate;

                fala.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult, 0, new System.Globalization.CultureInfo("pt-BR"));

                Console.CancelKeyPress += new ConsoleCancelEventHandler(OnExit);
                Console.WriteLine("Monitorando a área de transferência... Pressione Ctrl+C para sair. Pressione Ctrl+P para pausar/continuar.");

                // Iniciar thread para monitorar o clipboard
                Thread clipboardThread = new Thread(MonitorClipboard);
                clipboardThread.SetApartmentState(ApartmentState.STA); // Clipboard requires STA thread
                clipboardThread.Start();

                // Iniciar thread para monitorar o teclado
                Thread keyPressThread = new Thread(MonitorKeyPress);
                keyPressThread.Start();

                clipboardThread.Join();
                keyPressThread.Join();
            }
        }

        private static void MonitorClipboard()
        {
            while (running)
            {
                Thread.Sleep(250);

                if (paused)
                {
                    continue;
                }

                try
                {
                    string? texto = null;

                    // Acessar a área de transferência dentro de um try/catch
                    if (Clipboard.ContainsText())
                    {
                        texto = Clipboard.GetText();
                    }

                    if (string.IsNullOrEmpty(texto))
                    {
                        continue;
                    }

                    if (texto == ultimoTexto)
                    {
                        continue;
                    }

                    ultimoTexto = texto;
                    Falar(texto);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro ao acessar a área de transferência: " + ex.Message);
                }
            }
        }

        private static void Falar(string texto)
        {
            string ssmlText = $@"
                <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='pt-BR'>
                    <prosody pitch='{velocidade}'>
                        {System.Security.SecurityElement.Escape(texto)}
                    </prosody>
                </speak>";

            fala.SpeakSsmlAsync(ssmlText);
        }

        private static void MonitorKeyPress()
        {
            while (running)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.P)
                    {
                        TogglePause();
                    }
                }

                Thread.Sleep(100); // Pequena pausa para não sobrecarregar a CPU
            }
        }

        private static void TogglePause()
        {
            paused = !paused;

            if (paused)
            {
                fala.Pause();
                Console.WriteLine("Fala pausada.");
            }
            else
            {
                fala.Resume();
                Console.WriteLine("Fala retomada.");
            }
        }

        private static void OnExit(object sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;
            running = false;
            fala.Dispose();
            Console.WriteLine("Encerrando o programa...");
        }
    }
}
