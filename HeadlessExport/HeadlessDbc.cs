using SpellEditor.Sources.Database;
using SpellEditor.Sources.DBC;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HeadlessExport
{
    class HeadlessDbc : AbstractDBC
    {
        public int TaskId;
        public Stopwatch Timer;

        public async Task<Stopwatch> TimedExportToDBC(IDatabaseAdapter adapter, string IdKey, string bindingName)
        {
            Timer = new Stopwatch();
            Timer.Start();
            var task = ExportToDbc(adapter, Program.SetProgress, IdKey, bindingName);
            TaskId = task.Id;
            await task;
            Timer.Stop();
            return Timer;
        }
    }
}
