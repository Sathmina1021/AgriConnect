using System;
using System.Runtime.Serialization;

namespace Farmer.Contracts
{
    /// <summary>Public profile of a farmer (never contains password data).</summary>
    [DataContract(Namespace = FarmerNamespaces.Data)]
    public class FarmerDto
    {
        [DataMember] public int FarmerId { get; set; }
        [DataMember] public string FullName { get; set; }
        [DataMember] public string Email { get; set; }
        [DataMember] public string Phone { get; set; }
        [DataMember] public string District { get; set; }
        [DataMember] public string Address { get; set; }
        [DataMember] public DateTime RegisteredOn { get; set; }
    }

    [DataContract(Namespace = FarmerNamespaces.Data)]
    public class FarmerRegistration
    {
        [DataMember] public string FullName { get; set; }
        [DataMember] public string Email { get; set; }
        [DataMember] public string Password { get; set; }
        [DataMember] public string Phone { get; set; }
        [DataMember] public string District { get; set; }
        [DataMember] public string Address { get; set; }
    }

    [DataContract(Namespace = FarmerNamespaces.Data)]
    public class LoginCredentials
    {
        [DataMember] public string Email { get; set; }
        [DataMember] public string Password { get; set; }
    }

    [DataContract(Namespace = FarmerNamespaces.Data)]
    public class LoginResult
    {
        [DataMember] public bool Success { get; set; }
        [DataMember] public string Message { get; set; }
        /// <summary>Populated only when Success is true.</summary>
        [DataMember] public FarmerDto Profile { get; set; }
    }
}
