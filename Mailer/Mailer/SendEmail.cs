﻿using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Mailer
{
    internal class SendEmail
    {
        internal static void Register(CommandLineApplication app)
        {
            app.Command("send", cmd =>
            {
                cmd.Description = "Tests a connection to an endpoint";
               
                var serverOption = cmd.Option("-h | --server", "The SMTP Server", CommandOptionType.SingleValue);
                var enableSslOption = cmd.Option("-s | --ssl", "Secure with ssl", CommandOptionType.NoValue);
                var fromOption = cmd.Option("-f | --from", "Email from", CommandOptionType.SingleValue);
                var toOption = cmd.Option("-t | --to", "Email to", CommandOptionType.MultipleValue);
                var portOption = cmd.Option("-p | --port", "Port", CommandOptionType.SingleValue);
                var subjectOption = cmd.Option("-su | --subject", "Subject", CommandOptionType.SingleValue);
                var bodyOption = cmd.Option("-b | --body", "Body", CommandOptionType.SingleValue);
                var usernameOption = cmd.Option("-u | --username", "Username", CommandOptionType.SingleValue);
                var passwordOption = cmd.Option("-pw | --password", "Password", CommandOptionType.SingleValue);
                var authenticationTypeOption = cmd.Option("-a | --authenticationType", "AuthenticationType", CommandOptionType.SingleValue);

            cmd.OnExecute(() => ExecuteAsync(serverOption.Value(),
                                                int.Parse(portOption.Value()),
                                                fromOption.Value(),
                                                toOption.Values,
                                                subjectOption.Value(),
                                                bodyOption.Value(),
                                                usernameOption.Value(),
                                                passwordOption.Value(),
                                                enableSslOption.HasValue(),
                                                authenticationTypeOption.Value()));
            });
        }

        private static IServiceProvider ConfigureAndBuild()
        {
            var services = new ServiceCollection();

            // Add dependencies
            services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Debug));
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddOptions();


            return services.BuildServiceProvider();
        }

  

        public static async Task<int> ExecuteAsync(string server, int port, string from, List<string> to, string subject, string body, string username, string password, bool useSsl, string authType)
        {
            var provider = ConfigureAndBuild();
            var logger = provider.GetRequiredService<ILogger<SendEmail>>();
            try
            {
                if (to != null && to.Any() && !string.IsNullOrWhiteSpace(subject) && !string.IsNullOrWhiteSpace(body))
                {

                    if (!string.IsNullOrWhiteSpace(server) && !string.IsNullOrWhiteSpace(from))
                    {
                        using (var smtpServer = new SmtpClient(server, port))
                        {
                            var message = new MailMessage
                            {
                                From = new MailAddress(from),
                                Subject = subject,
                                Body = body
                            };
                            message.ReplyToList.Add(message.From);
                            foreach (var item in to)
                            {
                                try
                                {
                                    message.To.Add(item);
                                }
                                catch (FormatException)
                                {
                                    logger.LogError("Invalid To Address: {0}", item);
                                    return 1;
                                }
                            }


                            if (message.To.Any())
                            {
                                smtpServer.EnableSsl = useSsl;
                                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                                {
                                    smtpServer.UseDefaultCredentials = false;
                                    smtpServer.Credentials = new System.Net.NetworkCredential(username, password);                                   
                                }

                                if (!string.IsNullOrWhiteSpace(authType))
                                {
                                    smtpServer.Credentials = smtpServer.Credentials.GetCredential(server, smtpServer.Port, authType);
                                }
                                
                                await smtpServer.SendMailAsync(message);

                                logger.LogInformation($"Mail sent successfully: {message}");
                            }
                            else
                            {
                                logger.LogError("Did not send an email because To list is empty");
                                return 1;
                            }
                        }
                    }

                }
                else
                {
                    logger.LogError("Missing email settings. Please check To, Subject and Body!");
                    return 1;
                }

            }
            catch (Exception ex)
            {
                logger.LogError($"Exception occured: {ex.Message}");
                return 1;
            }

            return 0;
        }
    }
}
