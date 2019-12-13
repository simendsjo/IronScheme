#region License
/* Copyright (c) 2007-2016 Llewellyn Pritchard
 * All rights reserved.
 * This source code is subject to terms and conditions of the BSD License.
 * See docs/license.txt. */
#endregion

using System;
using IronScheme.Runtime;
using Microsoft.Scripting.Hosting;
using System.Reflection;

namespace IronScheme.Hosting
{
  public sealed class IronSchemeConsoleHost : ConsoleHost
  {
    internal static string VERSION = GetVersion();

    static string GetVersion()
    {
      foreach (var a in typeof(Builtins).Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false))
      {
        var info = (AssemblyInformationalVersionAttribute)a;
        if (info.InformationalVersion == "1.0.0.0")
        {
          return "Latest";
        }
        return info.InformationalVersion;
      }
      return "Latest";
    }

    string logo;
    public IronSchemeConsoleHost()
    {
      logo = string.Format("IronScheme {0} github.com/IronScheme/IronScheme � 2007-2019 Llewellyn Pritchard ", VERSION);
    }

    public static int Execute(string[] args)
    {
      return new IronSchemeConsoleHost().Run(args);
    }
    
    protected override void Initialize()
    {
      base.Initialize();
      Options.LanguageProvider = ScriptEnvironment.GetEnvironment().GetLanguageProvider(typeof(IronSchemeLanguageProvider));
    }

    protected override void PrintLogo()
    {
      if (Options.RunAction != ConsoleHostOptions.Action.RunFiles && !LanguageProvider.InputRedirected)
      {
        // errrkkk
        var tokens = logo.Split(new string[] { "github.com/IronScheme/IronScheme" }, StringSplitOptions.None);

        ConsoleColor old = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(tokens[0]);
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("github.com/IronScheme/IronScheme");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(tokens[1]);

        Console.ForegroundColor = old;

        var version = Environment.Version.ToString(2);

        var ass = typeof(object).Assembly;

        var isCore = ass.FullName.StartsWith("System.Private.CoreLib");

        if (isCore)
        {
          var va =
            ((AssemblyFileVersionAttribute) ass.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false)[0])
            ?.Version ?? string.Empty;
          var v = new Version(va);
          if (v.Minor == 6)
          {
            if (v.MajorRevision < 26515)
            {
              version = "Core 2.0";
            }
            else
            {
              version = "Core 2.1";
            }
          }
          else if (v.Minor == 7)
          {
            version= "Core 3.0";
          }
          else
          {
            version = "Core " + version;
          }
        }

        Console.WriteLine("(.NET {1} {0})", IntPtr.Size == 8 ? "64-bit" : "32-bit", version);
        
      }
    }
  }
}
