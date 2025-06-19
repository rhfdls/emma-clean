namespace Emma.Models.Enums
{
    /// <summary>
    /// Defines the type of collaboration a user can have with a contact
    /// </summary>
    public enum ContactCollaborationType
    {
        /// <summary>
        /// Has full read/write access to the contact
        /// </summary>
        FullAccess = 1,
        
        /// <summary>
        /// Can view and edit the contact but cannot delete or transfer ownership
        /// </summary>
        EditAccess = 2,
        
        /// <summary>
        /// Can only view the contact's information
        /// </summary>
        ViewOnly = 3,
        
        /// <summary>
        /// Can only view the contact's information and add notes
        /// </summary>
        NoteOnly = 4,
        
        /// <summary>
        /// Temporary access that will expire after a set time
        /// </summary>
        Temporary = 5
    }
}
