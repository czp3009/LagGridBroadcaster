using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRageMath; // Para utilizar VRageMath.Color

namespace LagGridBroadcaster
{
    internal class GpsComparer : IEqualityComparer<IMyGps>
    {
        public bool Equals(IMyGps x, IMyGps y)
        {
            if (x == y)
            {
                x.GPSColor = Color.Red; // Definindo a cor vermelha
                return true;
            }
            if (x == null || y == null) return false;
            return x.Hash == y.Hash;
        }

        public int GetHashCode(IMyGps obj)
        {
            return obj.Hash;
        }
    }

    public class GpsManager
    {
        public IMyGps CreateOrUpdateGps(Vector3D position, string name, long entityId, Color color)
        {
            // Criar um novo GPS ou obter um existente
            IMyGps gps = MyAPIGateway.Session.GPS.Create(name, "", position, true);
            gps.GPSColor = color;

            // Adicionar o GPS ao jogador
            MyAPIGateway.Session.GPS.AddGps(entityId, gps);
            return gps;
        }
    }

    public static class GpsHelper
    {
        public static void UpdateGpsForPlayer(long playerId, Vector3D position, string name)
        {
            GpsManager gpsManager = new GpsManager();
            Color redColor = new Color(255, 0, 0); // Cor vermelha
            gpsManager.CreateOrUpdateGps(position, name, playerId, redColor);
        }
    }
}
