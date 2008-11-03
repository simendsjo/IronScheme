#region License
/* ****************************************************************************
 * Copyright (c) Llewellyn Pritchard. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. 
 * A copy of the license can be found in the License.html file at the root of this distribution. 
 * By using this source code in any fashion, you are agreeing to be bound by the terms of the 
 * Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 * ***************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Shell;
using System.IO;
using IronScheme.Runtime;
using System.Threading;

namespace IronScheme.Hosting
{
  public sealed class IronSchemeLanguageProvider : LanguageProvider
  {
    public IronSchemeLanguageProvider(ScriptDomainManager x)
      : base(x)
    {
#if !DEBUG
      ScriptDomainManager.Options.DebugMode = false;
      ScriptDomainManager.Options.EngineDebug = false;
      ScriptDomainManager.Options.DebugCodeGeneration = false;
      ScriptDomainManager.Options.OptimizeEnvironments = true;

#endif
      ScriptDomainManager.Options.AssemblyGenAttributes =
#if DEBUG
 Microsoft.Scripting.Generation.AssemblyGenAttributes.EmitDebugInfo |
        Microsoft.Scripting.Generation.AssemblyGenAttributes.GenerateDebugAssemblies |
#endif
 Microsoft.Scripting.Generation.AssemblyGenAttributes.DisableOptimizations |
        Microsoft.Scripting.Generation.AssemblyGenAttributes.SaveAndReloadAssemblies;

      ScriptDomainManager.Options.DynamicStackTraceSupport = false;

      x.RegisterLanguageProvider("IronScheme", "IronScheme.Hosting.IronSchemeLanguageProvider", ".scm", ".ss", ".sch", ".sls");
      Runtime.Closure.ConsFromArray = Runtime.Cons.FromArray;
      Runtime.Closure.ConsStarFromArray = delegate(object[] args) { return Builtins.ToImproper(Cons.FromArray(args)); };
      Runtime.Closure.Unspecified = Builtins.Unspecified;
    }

    public override string LanguageDisplayName
    {
      get { return "IronScheme"; }
    }

    IronSchemeScriptEngine se;

    public override ScriptEngine GetEngine(EngineOptions options)
    {
      if (se == null)
      {
        LanguageContext lc = new IronSchemeLanguageContext();
        se = new IronSchemeScriptEngine(this, options ?? GetOptionsParser().EngineOptions, lc);
      }
      return se;
    }

    public override IConsole GetConsole(CommandLine commandLine, IScriptEngine engine, ConsoleOptions options)
    {
      return base.GetConsole(commandLine, engine, options);
    }

    CommandLineX cl = new CommandLineX();

    public IConsole Console
    {
      get
      {
        return cl.GetConsole();
      }

    }

    public override CommandLine GetCommandLine()
    {
      return cl;
    }

    class CommandLineX : CommandLine
    {
      protected override string Prompt
      {
        get { return "> "; }
      }

      internal IConsole GetConsole()
      {
        return Console;
      }

      protected override string PromptContinuation
      {
        get { return ". "; }
      }

      protected override void Initialize()
      {
        if (File.Exists("unknown_ast.ast"))
        {
          File.Delete("unknown_ast.ast");
        }
        base.Initialize();
      }

      protected override int RunFile(string filename)
      {
        IronScheme.Runtime.Builtins.commandline = Options.RemainingArgs;
        Runtime.Builtins.Load("~/ironscheme.boot.pp");
        int ev = 0;
        AutoResetEvent e = new AutoResetEvent(false);
        Thread t = new Thread(delegate ()
          {
            try
            {
              Engine.Execute(string.Format("(load \"{0}\")", filename.Replace('\\', '/')));
              ev = 0;
            }
            catch (ThreadAbortException)
            {
              System.Console.Error.WriteLine("Execution interupted");
              ev = 2;
            }
            catch (Exception ex)
            {
              System.Console.Error.WriteLine(ex);
              ev = 1;
            }
            finally
            {
              e.Set();
            }
            //good for now
          }, 5000000);

        t.Start();

        System.Console.CancelKeyPress += delegate
        {
          t.Abort();
        };

        e.WaitOne();

        return ev;
      }

      protected override void OnInteractiveLoopStart()
      {
        IronScheme.Runtime.Builtins.commandline = new string[] { "interactive" };
#if CPS
        Runtime.Builtins.Load("~/ironscheme.boot.cps");
#else
        Runtime.Builtins.Load("~/ironscheme.boot.pp");
#endif
        if (File.Exists("init.ss"))
        {
#if CPS
          Engine.Execute("(eval-r6rs values '(include \"init.ss\"))", Compiler.BaseHelper.scriptmodule);
#else
          Engine.Execute("(eval-r6rs '(include \"init.ss\"))", Compiler.BaseHelper.scriptmodule);
#endif
        }

      }
    }

    public override OptionsParser GetOptionsParser()
    {
      return new IronSchemeOptionsParser();
    }

    class IronSchemeOptionsParser : OptionsParser
    {
      public override void GetHelp(out string commandLine, out string[,] options, out string[,] environmentVariables, out string comments)
      {
        commandLine = null;
        options = null;
        environmentVariables = null;
        comments = null;
      }

      bool notabcompletion = false;

      protected override void ParseArgument(string arg)
      {
        if (arg == "-notabcompletion")
        {
          notabcompletion = true;
        }
        else
        {
          base.ParseArgument(arg);
        }
      }

      class IronSchemeConsoleOptions : ConsoleOptions
      {
      }

      ConsoleOptions co;

      public override Microsoft.Scripting.Shell.ConsoleOptions ConsoleOptions
      {
        get
        {
          if (co == null)
          {
            co = new IronSchemeConsoleOptions();
            co.TabCompletion = !notabcompletion;
            co.ColorfulConsole = !notabcompletion;
          }
#if DEBUG
          //co.HandleExceptions = false;
#endif
          return co;
        }
        set
        {
          base.ConsoleOptions = value;
        }
      }

      class IronSchemeEngineOptions : EngineOptions
      {
      }

      public override EngineOptions EngineOptions
      {
        get
        {
          EngineOptions eo = new IronSchemeEngineOptions();
          eo.ProfileDrivenCompilation = false;
          //eo.InterpretedMode = true;
          // this will blow up visual studio with anything but small files
          //eo.ClrDebuggingEnabled = true;

          return eo;
        }
        set
        {
          base.EngineOptions = value;
        }
      }
    }
  }
}
