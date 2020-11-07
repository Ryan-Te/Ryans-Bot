using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Net.Http.Headers;
using System.Threading.Tasks.Dataflow;
using System.Runtime.ConstrainedExecution;
using System.Globalization;
using System.IO;

//Bot Invite Code https://discord.com/api/oauth2/authorize?client_id=768566038395879484&permissions=1812462657&scope=bot

class Program
{
    public static void Main(string[] args)
	=> new Program().MainAsync().GetAwaiter().GetResult();

    private Task Log(LogMessage msg)
	{
		Console.WriteLine(msg.ToString());
		return Task.CompletedTask;
	}

    private DiscordSocketClient _client;
    private CommandHandler _cmd;
	public async Task MainAsync()
	{
		_client = new DiscordSocketClient();
        _cmd = new CommandHandler(_client, new CommandService());

		_client.Log += Log;

        var authToken = File.ReadAllText($"{Directory.GetCurrentDirectory()}/auth token.txt");
        await _client.LoginAsync(TokenType.Bot, authToken);
		await _client.StartAsync();
        await _cmd.InstallCommandsAsync();

        await Task.Delay(-1);
	}
}


public class CommandHandler
{
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commands;


    public CommandHandler(DiscordSocketClient client, CommandService commands)
    {
        _commands = commands;
        _client = client;
    }

    public async Task InstallCommandsAsync()
    {
        // Hook the MessageReceived event into our command handler
        _client.MessageReceived += HandleCommandAsync;

        // Here we discover all of the command modules in the entry 
        // assembly and load them. Starting from Discord.NET 2.0, a
        // service provider is required to be passed into the
        // module registration method to inject the 
        // required dependencies.
        //
        // If you do not use Dependency Injection, pass null.
        // See Dependency Injection guide for more information.
        await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                        services: null);
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
        // Don't process the command if it was a system message
        var message = messageParam as SocketUserMessage;
        if (message == null) return;

        // Create a number to track where the prefix ends and the command begins
        int argPos = 0;

        // Determine if the message is a command based on the prefix and make sure no bots trigger commands
        if (!(message.HasStringPrefix("!rtb ", ref argPos) ||
            message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
            message.Author.IsBot)
            return;

        // Create a WebSocket-based command context based on the message
        var context = new SocketCommandContext(_client, message);

        // Execute the command with the command context we just
        // created, along with the service provider for precondition checks.

        // Keep in mind that result does not indicate a return value
        // rather an object stating if the command executed successfully.
        var result = await _commands.ExecuteAsync(
            context: context,
            argPos: argPos,
            services: null);

        // Optionally, we may inform the user if the command fails
        // to be executed; however, this may not always be desired,
        // as it may clog up the request queue should a user spam a
        // command.
        if (!result.IsSuccess)
        await context.Channel.SendMessageAsync($"There was a error!: {result.ErrorReason}");
    }
}

public class TextModule : ModuleBase<SocketCommandContext>
{
    [Command("echo")]
    [Summary("Echoes a message")]
    public Task EchoAsync([Remainder] [Summary("The text to echo")]string echo)
        => ReplyAsync(echo);

    [Command("ping")]
    [Summary("Returns \"Pong!\"")]
    public Task PingAsync()
        => ReplyAsync("Pong!");


    [Command("help")]
    [Summary("Shows commands")]
    public Task HelpAsync()
	{
        return ReplyAsync($" COMMANDS: \n echo: Echoes a message \n ping: Returns \"Pong\" \n help: Shows commands \n oldmath: Does simple math \n math: Does multi step math");
	}
}

public class MathModule : ModuleBase<SocketCommandContext>
{
    [Command("oldmath")]
    [Summary("Does simple math")]

    public Task MathAsync([Summary("The 1st number")] double A, [Summary("The operation")] string Op, [Summary("The 2nd number")] double B)
	{
        double Out = 0;
        string Error = "";
        if(Op == "+")
		{
            Out = A + B;
		}
        else if (Op == "-")
        {
            Out = A - B;
        }
        else if (Op == "*")
        {
            Out = A * B;
        }
        else if (Op == "/")
        {
            if(B != 0)
            {
                Out = A / B;
			}
			else
			{
                Error = "Divide By 0";
			}
        }
        else if (Op == "^")
        {
            Out = Math.Pow(A, B);
        }
        else if (Op == "root")
        {
            Out = Math.Pow(B, 1d / A);
        }
        else
		{
            Error = "Function Not Valid";
		}
		if (Error == "")
		{
			return ReplyAsync($"{A} {Op} {B} = {Out}");
		}
        return ReplyAsync($"Math Error: {Error}");
    }

    [Command("oldmath")]
    [Summary("Does simple math")]

    public Task MathAsync([Summary("The operation")] string Op, [Summary("The 2nd number")] double B)
	{
        double Out = 0;
        string Error = "";
        if (Op == "sqrt")
        {
            Out = Math.Sqrt(B);
        }
        else if (Op == "cbrt")
		{
            Out = Math.Pow(B, 1d / 3d);
        }
        else
        {
            Error = "Function Not Valid";
        }
        if (Error == "")
        {
            return ReplyAsync($"{Op} {B} = {Out}");
        }
        return ReplyAsync($"Math Error: {Error}");
    }


    [Command("oldmath")]
    [Summary("Does simple math")]

    public Task MathAsync()
	{
        return ReplyAsync($"Included Operations: \n +: Add \n -: Subtract \n *: Multiply \n /: Divide \n ^: Exponent \n root: Nth root \n sqrt: Square root \n cbrt: Cube root");

    }

    string[] Conts = {"pi", "π", "rho", "e", "i"};
    string[] Vals = {$"rI{Math.PI};0", $"rI{Math.PI};0", $"rI{100d};0", $"rI{Math.E};0", "rI0;1"};

    string[] Ops = {"(", ")", "sqrt", "cbrt", "root", "^", "*", "/", "+", "-"};

    string[] Tags = {"nofloatingerrors", "showwork"};
    [Command("math")]
    [Summary("Does multi step math")]

    public Task NewMathAsync([Remainder][Summary("The equation to solve")] string Eq)
	{
        bool noFloatingErrors = false;
        bool showWork = false;

        while (Eq.Contains("\\"))
        {
            int first = Eq.IndexOf("\\");
            int n = 0;
            foreach (string t in Tags)
            {
                if (first + 1 + t.Length <= Eq.Length)
                {
                    if ((Eq.ToLower()).Substring(first + 1, t.Length) == t)
                    {
                        Eq = Eq.Substring(0, first) + Eq.Substring(first + 1 + t.Length);
                        if (n == 0)
                        {
                            noFloatingErrors = true;
                        }
                        if (n == 1)
                        {
                            showWork = true;
                        }
                    }
                }
                n++;
            }
            int newfirst = Eq.IndexOf("\\");
            if(first == newfirst)
			{
                return ReplyAsync($"Math Error: Invalid Tag");
			}
        }
        string EqC = Eq;
        EqC = EqC.ToLower();
		while (EqC.Contains(" "))
		{
			int first = EqC.IndexOf(" ");
			var EqCN = EqC.Substring(0, first) + EqC.Substring(first + 1);
            EqC = EqCN;
		}
        int i = 0;
        
		foreach(string c in Conts)
		{
            while (EqC.Contains(c))
            {
                int first = EqC.IndexOf(c);
                var EqCN = EqC.Substring(0, first) + Vals[i] + EqC.Substring(first + c.Length);
                EqC = EqCN;
                //Console.WriteLine(EqC);
                //return ReplyAsync("Pls don't use me rn. Ryan is debugging");
            }
            i++;
        }
        foreach (string op in Ops)
        {
            var EqT = EqC;
            while (EqT.Contains(op))
            {
                int first = EqT.IndexOf(op);
                var comma1 = ",";
                var comma2 = ",";
                if(op == "(")
				{
                    comma1 = "";
				}
                if (op == ")")
                {
                    comma2 = "";
                }
                var EqCN = EqC.Substring(0, first) + comma1 + EqC.Substring(first, op.Length) + comma2 + EqC.Substring(first + op.Length);
                var commas = ",";
                for(int m = 1; m < op.Length; m++)
				{
                    commas = commas + ",";
				}

                EqT = EqT.Substring(0, first) + comma1 + commas + comma2 + EqC.Substring(first + op.Length);
                EqC = EqCN;
            }
        }
        List<string> tokens = EqC.Split(",").OfType<string>().ToList();
        int j = 0;
        while (j < tokens.Count)
        {
            var t = tokens[j];
            if(t == "-")
			{
                var opq = false;
                var ta = false;
                foreach(string opp in Ops)
				{
                    if (j - 1 >= 0)
                    {
                        if (opp == tokens[j - 1] && opp != ")")
                        {
                            opq = true;
                        }
                        if ("" == tokens[j - 1])
                        {
                            if (j - 2 >= 0)
							{
                                if (opp == tokens[j - 2] && opp != ")")
                                {
                                    opq = true;
                                    ta = true;
                                }
                            }
                        }
                    }
				}
                if(j == 0 || opq)
				{
                    if (!ta)
                    {
                        tokens.RemoveAt(j);
                        tokens[j] = "-" + tokens[j];
					}
					else
					{
                        tokens.RemoveAt(j - 1);
                        tokens.RemoveAt(j - 1);
                        tokens[j - 1] = "-" + tokens[j - 1];
                    }
                }
			}
            j++;
        }

        var Error = "";
        Complex getNumber(List<string> c, int id)
		{
			double Out = 1;
            if (c[id].Length >= 3)
            {
                try
                {
                    if (c[id].Substring(0, 2) == "rI")
                    {
                        var semi = c[id].IndexOf(";");
                        //Console.WriteLine(c[id].Substring(3, semi - 2));
                        // Console.WriteLine(c[id].Substring(semi + 1));
                        double real = Convert.ToDouble(c[id].Substring(2, semi - 2));
                        double imaginary = Convert.ToDouble(c[id].Substring(semi + 1));
                        return new Complex(real, imaginary);
                    }
                }
				catch (System.FormatException)
				{

				}
            }
            try
			{
                Out = Convert.ToDouble(c[id]);
            }
			catch (System.FormatException)
			{
                Error = "Invalid Equation";
			}
            return new Complex(Out, 0);
		}

        var work = new List<string>();

        void log(List<string> c, string b4b, string afb)
		{
            var printout = $"= {b4b}";
            var o = 0;
            foreach (string w in c)
            {
                Console.Write($"{w}, ");
                var nw = w;
                bool parseComplex = false;
                if (nw.Length > 1)
                {
                    if (nw.Substring(0, 2) == "rI")
                    {
                        parseComplex = true;
                    }
                }
                if (parseComplex)
                {
                    Complex num = getNumber(c, o);
                    if (Complex.Abs(num.Real) < 0.00000000001d && noFloatingErrors)
                    {
                        num = new Complex(0, num.Imaginary);
                    }
                    if (Complex.Abs(num.Imaginary) < 0.00000000001d && noFloatingErrors)
                    {
                        num = new Complex(num.Real, 0);
                    }
                    string res = "";
                    if (num.Real != 0)
                    {
                        res = $"{num.Real} + ";
                    }
                    if (num.Real != 0 && num.Imaginary == 00)
                    {
                        res = $"{num.Real} ";
                    }
                    string ims = $"";
                    if (num.Imaginary != 0 && num.Imaginary != 1)
                    {
                        ims = $"{num.Imaginary}i";
                    }
                    if (num.Imaginary == 1)
                    {
                        ims = "i";
                    }
                    nw = res + ims;
                }
                printout = printout + $"{nw} ";
                o++;
            }
            printout = printout + afb;
            work.Add(printout);
            Console.WriteLine();
        }

        Complex Compute(List<string> c, string b4b, string afb)
        {
            Console.WriteLine("New Equation");
            foreach (string w in c)
            {
                Console.Write($"{w}, ");
            }
            Console.WriteLine();
            foreach (string op in Ops)
            {
                int k = 0;
                while(k < c.Count) 
                {
                    var t = c[k];
                    if (t == op)
                    {
                        if (op == "(")
                        {
                            Console.WriteLine(c.Count);
                            List<string> ttc = new List<string>();
                            int brakets = 1;
                            var end = 0;
                            for(int l = k + 1; k < c.Count; l++)
							{
                                if(c[l] == "(")
								{
                                    brakets++;
								}
                                if (c[l] == ")")
                                {
                                    brakets--;
                                }
                                if(brakets < 1)
								{
                                    end = l;
                                    break;
								}
                                ttc.Add(c[l]);
                            }
                            var ttcc = ttc.Count;
                            Console.WriteLine(ttcc);
                            var output = Compute(ttc, "", "");
                            c.RemoveRange(k, ttcc + 2);
                            c.Insert(k, $"rI{output.Real};{output.Imaginary}");
                        }
                        if (op == "sqrt")
                        {
                            var B = getNumber(c, k + 1);
                            var Out = Complex.Sqrt(B);
                            c.RemoveRange(k - 1, 3);
                            c.Insert(k - 1, $"rI{Out.Real};{Out.Imaginary}");
                            //k--;
                            log(c, b4b, afb);
                        }
                        if (op == "cbrt")
                        {
                            var B = getNumber(c, k + 1);
                            var Out = Complex.Pow(B, 1d / 3d);
                            c.RemoveRange(k - 1, 3);
                            c.Insert(k - 1, $"rI{Out.Real};{Out.Imaginary}");
                            //k--;
                            log(c, b4b, afb);
                        }
                        if (op == "root")
                        {
                            var A = getNumber(c, k - 1);
                            var B = getNumber(c, k + 1);
                            var Out = Complex.Pow(B, 1d / A);
                            c.RemoveRange(k - 1, 3);
                            c.Insert(k - 1, $"rI{Out.Real};{Out.Imaginary}");
                            k--;
                            log(c, b4b, afb);
                        }
                        if (op == "^")
                        {
                            var A = getNumber(c, k - 1);
                            var B = getNumber(c, k + 1);
                            Complex Out = Complex.Pow(A, B);
                            c.RemoveRange(k - 1, 3);
                            c.Insert(k - 1, $"rI{Out.Real};{Out.Imaginary}");
                            k--;
                            log(c, b4b, afb);
                        }
                        if (op == "*")
                        {
                            var A = getNumber(c, k - 1);
                            var B = getNumber(c, k + 1);
                            Complex Out = A * B;
                            c.RemoveRange(k - 1, 3);
                            c.Insert(k - 1, $"rI{Out.Real};{Out.Imaginary}");
                            k--;
                            log(c, b4b, afb);
                        }
                        if (op == "/")
                        {
                            var A = getNumber(c, k - 1);
                            var B = getNumber(c, k + 1);
                            Complex Out = 0;
                            if (B != 0)
                            {
                                Out = A / B;
                            }
                            else
                            {
                                Error = "Divide By 0";
                                return 0d;
                            }
                            c.RemoveRange(k - 1, 3);
                            c.Insert(k - 1, $"rI{Out.Real};{Out.Imaginary}");
                            k--;
                            log(c, b4b, afb);
                        }
                        if (op == "+")
                        {
                            var A = getNumber(c, k - 1);
                            var B = getNumber(c, k + 1);
                            Complex Out = A + B;
                            c.RemoveRange(k - 1, 3);
                            c.Insert(k - 1, $"rI{Out.Real};{Out.Imaginary}");
                            k--;
                            log(c, b4b, afb);
                        }
                        if (op == "-")
                        {
                            var A = getNumber(c, k - 1);
                            var B = getNumber(c, k + 1);
                            Complex Out = A - B;
                            c.RemoveRange(k - 1, 3);
                            c.Insert(k - 1, $"rI{Out.Real};{Out.Imaginary}");
                            k--;
                            log(c, b4b, afb);
                        }
                    }
                    k++;
                }
            }
            if(c.Count == 1)
			{
                return getNumber(c, 0);

            }
            Error = "Invalid Equation";
            return new Complex(0,0);
        }
        var val = Compute(tokens, "", "");
        if(Complex.Abs(val.Real) < 0.00000000001d && noFloatingErrors)
		{
            val = new Complex(0, val.Imaginary);
		}
        if (Complex.Abs(val.Imaginary) < 0.00000000001d && noFloatingErrors)
        {
            val = new Complex(val.Real, 0);
        }
        string res = "";
        if (val.Real != 0)
        {
            res = $"{val.Real} + ";
        }
        if(val.Real != 0 && val.Imaginary == 00)
		{
            res = $"{val.Real} ";
        }
        string ims = $"";
        if(val.Imaginary != 0 && val.Imaginary != 1)
		{
            ims = $"{val.Imaginary}i";
		}
        if(val.Imaginary == 1)
		{
            ims = "i";
		}
        if (Error == "")
        {
            if (showWork)
            {
                string workS = $"{Eq}\n";
                foreach (string message in work)
                {
                    workS = workS + $"{message}\n";
                }
                return ReplyAsync(workS);
            }
            else
            {
                return ReplyAsync($"{Eq} = {res}{ims}");
            }
		}
		else
		{
            return ReplyAsync($"Math Error: {Error}");
		}
	}
    [Command("math")]
    [Summary("Does multi step math")]

    public Task NewMathAsync()
	{
        return ReplyAsync($"Included Operations: \n +: Add \n -: Subtract \n *: Multiply \n /: Divide \n ^: Exponent \n root: Nth root \n sqrt: Square root \n cbrt: Cube root \n \n Included Constants: \n pi: the ratio of a circle's circumference to its diameter \n π: same as pi \n rho: 100 \n e: Eulers number \n i: sqrt(-1) \n \n Included Tags \n \\noFloatingErrors: Removes very small numbers likely caused by floating point rounding errors");
    }

}



