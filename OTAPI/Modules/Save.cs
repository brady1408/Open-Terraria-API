using System;
using Mod.Framework;

namespace OTAPI.Modules
{
    /// <summary>
    /// This module will save any cecil changes to disk. This is generally the last module to run.
    /// </summary>
    [Module("File save", "death", 100)]
    public class SaveModule : RunnableModule
    {
        private ModFramework _framework;

        public SaveModule(ModFramework framework)
        {
            _framework = framework;
        }

        public override void Run()
        {
            var save_directory = "Output";
            System.IO.Directory.CreateDirectory(save_directory);
            foreach (var asm in this._framework.CecilAssemblies)
            {
                var save_to = System.IO.Path.Combine(save_directory, asm.MainModule.Name);
                if (System.IO.File.Exists(save_to))
                    System.IO.File.Delete(save_to);

                asm.Write(save_to);
                Console.WriteLine($"Saved output file: {save_to}");
            }
        }
    }
}
