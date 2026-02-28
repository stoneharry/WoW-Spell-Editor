using SpellEditor.Sources.Database;
using SpellEditor.Sources.DBC;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace HeadlessExport
{
    class HeadlessDbc : AbstractDBC
    {
        public int TaskId;
        public Stopwatch Timer;

        public async Task<Stopwatch> TimedExportToDBC(IDatabaseAdapter adapter, string IdKey, string bindingName, ImportExportType type)
        {
            Timer = new Stopwatch();
            var task = ExportTo(adapter, Program.SetProgress, IdKey, bindingName, type);
            TaskId = task.Id;
            Timer.Start();
            await task;
            Timer.Stop();
            return Timer;
        }
    }
}
