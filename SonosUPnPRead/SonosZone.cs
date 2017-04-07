using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SonosUPNP
{
    [Serializable]
    [DataContract]
    public class SonosZone
    {
        private IList<SonosPlayer> cplayers = new List<SonosPlayer>();
        public SonosZone(string coordinator)
        {
            CoordinatorUUID = coordinator;
        }

        public void AddPlayer(SonosPlayer player)
        {
            if (player.UUID == CoordinatorUUID)
            {
                Coordinator = player;
            }
            cplayers.Add(player);
        }
        /// <summary>
        /// Coordinator der Zone
        /// </summary>
        [DataMember]
        public SonosPlayer Coordinator { get; set; }
        /// <summary>
        /// Eindeutige ID des Coordinators
        /// </summary>
        [DataMember]
        public string CoordinatorUUID { get; private set; }
        /// <summary>
        /// Liste mit allen Playern
        /// </summary>
        [DataMember]
        public IList<SonosPlayer> Players
        {
            get { return cplayers; }
            set { cplayers = value; }
        }
    }
}