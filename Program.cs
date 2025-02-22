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
        private static SpeechSynthesizer fala;

        [STAThread]
        public static void Main(string[] args)
        {
            ultimoTexto = Clipboard.GetText();

            fala = new SpeechSynthesizer(); // Inicializa fora do using para reutilizar
            fala.Rate = rate;

            if (args.Length > 0)
            {
                rate = int.Parse(args[0]);
            }

            fala.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult, 0, new System.Globalization.CultureInfo("pt-BR"));

            Console.CancelKeyPress += new ConsoleCancelEventHandler(OnExit);
            Console.WriteLine("Monitorando a área de transferência... Pressione Ctrl+C para sair. Ctrl+P para pausar/continuar e CTRL+R para cancelar todas as falas.");

            Thread clipboardThread = new Thread(MonitorClipboard);
            clipboardThread.SetApartmentState(ApartmentState.STA); // Clipboard requires STA thread
            clipboardThread.Start();

            Thread keyPressThread = new Thread(MonitorKeyPress);
            keyPressThread.Start();

            clipboardThread.Join();
            keyPressThread.Join();
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

                    if (Clipboard.ContainsText())
                    {
                        texto = Clipboard.GetText();
                    }

                    if (string.IsNullOrEmpty(texto))
                    {
                        continue;
                    }

                    if (texto != ultimoTexto)
                    {
                        while (fala.State == SynthesizerState.Speaking)
                        {
                            Thread.Sleep(100);
                        }

                        ultimoTexto = texto;
                        Falar(texto);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro ao acessar a área de transferência: " + ex.Message);
                }
            }
        }

        private static void Falar(string texto)
        {
            // Reinicializa o SpeechSynthesizer
            fala.Dispose();
            fala = new SpeechSynthesizer();
            fala.Rate = rate;
            fala.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult, 0, new System.Globalization.CultureInfo("pt-BR"));

            // Verifica se o sintetizador está pronto
            while (fala.State != SynthesizerState.Ready)
            {
                Console.WriteLine($"Estado atual: {fala.State}. Aguardando ficar Ready...");
                Thread.Sleep(50);
            }

            // Adiciona um ponto com pausa no início e pausas nos pontos do texto
            string ssmlText = $@"
                <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='pt-BR'>
                    <prosody pitch='x-low'>
                        i.<break time='500ms'/> 
                        {System.Security.SecurityElement.Escape(texto)}
                    </prosody>
                </speak>";

            Thread.Sleep(100);
            fala.SpeakSsml(ssmlText);
        }

        private static void MonitorKeyPress()
        {
            while (running)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);

                    if (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.R)
                    {
                        Console.WriteLine("Reiniciando a fala...");
                        Reiniciar();
                    }

                    if (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.P)
                    {
                        TogglePause();
                    }
                }

                Thread.Sleep(100);
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

        private static void Reiniciar()
        {
            fala.SpeakAsyncCancelAll();
            ultimoTexto = "";
            Console.WriteLine("Fala reiniciada. Aguardando novo texto da área de transferência.");
        }
    }
}