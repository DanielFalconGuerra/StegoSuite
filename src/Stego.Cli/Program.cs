using System.Text;
using Stego.Cli;

// La consola de Windows no usa UTF-8 por defecto: sin esto, tildes y emojis salen corruptos.
Console.OutputEncoding = Encoding.UTF8;

return StegoCli.Run(args, Console.Out, Console.Error, new ConsolePasswordSource());
