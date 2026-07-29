namespace NosGm.Cluster.Contracts.V1
{
    public enum ClusterNodeRole
    {
        Unspecified = 0,
        Master = 1,
        Login = 2,
        World = 3,
        AuthBridge = 4,
        Web = 5,
        AdminTool = 6,
        Worker = 7
    }
}
