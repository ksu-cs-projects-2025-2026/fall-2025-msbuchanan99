namespace Client.Models
{
    public enum ConfirmMode { 
        DeleteFloss, 
        MarkCompleted, 
        ReadMessage, 
        StrandsConfirm,
        NumFlossConfirm, 
        GeneralConfirm, 
        DeleteConfirm, 
        ResetConfirm
    }

    public enum FlossModalMode { Create, Edit }

    public enum OwnershipMode { Owned, Unowned, Both }
}
