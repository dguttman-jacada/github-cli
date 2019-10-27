using System.Threading.Tasks;

namespace github_cli.Workloads
{
    internal interface IWorkload
    {
        Task Execute();
        string Name { get; }
    }
}
