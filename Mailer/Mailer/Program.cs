using Microsoft.Extensions.CommandLineUtils;
using System;

namespace Mailer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                FullName = "Mailer",
                Description = "Tool for sending emails over commandline."
            };

            SendEmail.Register(app);

            app.Command("help", cmd =>
            {
                cmd.Description = "Get help for the application, or a specific command";

                var commandArgument = cmd.Argument("<COMMAND>", "The command to get help for");
                cmd.OnExecute(() =>
                {
                    app.ShowHelp(commandArgument.Value);
                    return 0;
                });
            });


            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 0;
            });

            app.Execute(args);
        }
    }
}
