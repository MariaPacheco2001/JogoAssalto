using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GolpeCalculado
{
    // Mapa do diário (US17): as zonas aparecem à medida que o jogador as visita.
    // No Fácil (FullMapFromStart) estão todas reveladas desde o início.
    // O mapa só mostra as zonas e o jogador: nunca o vigilante, as câmaras, os quadros ou a mala.
    public sealed partial class HeistGame
    {
        [Header("Mapa do diário (US17)")]
        [Tooltip("Zonas do mapa (objetos em Missão Golpe Calculado > Zonas do mapa).")] public MapZone[] mapZones = new MapZone[0];
        [Tooltip("Canto sudoeste (x, z) do mundo que o mapa representa. Tem de corresponder ao canto inferior esquerdo da Área do mapa.")]
        public Vector2 mapWorldMin = new Vector2(-25, -43);
        [Tooltip("Canto nordeste (x, z) do mundo que o mapa representa. Tem de corresponder ao canto superior direito da Área do mapa.")]
        public Vector2 mapWorldMax = new Vector2(60, 62);

        readonly HashSet<string> revealedZones = new HashSet<string>();

        public bool IsRevealed(string zoneId)
        {
            return revealedZones.Contains(zoneId);
        }

        public string[] RevealedZones => mapZones.Where(z => z != null && revealedZones.Contains(z.id)).Select(z => z.id).ToArray();

        // Verdadeiro se o ponto está dentro da zona com este id (falso se a zona não existir na cena).
        public bool InMapZone(string zoneId, Vector3 point)
        {
            return mapZones.Any(z => z != null && z.id == zoneId && z.Contains(point));
        }

        // O ponto que conta para o mapa: o jogador a pé ou a carrinha quando está a conduzir.
        public Vector3 MapPosition => InVan ? van.transform.position : player.transform.position;

        // Revela a zona onde o ponto está, se ainda não estava revelada.
        void RevealZonesAt(Vector3 point)
        {
            foreach (var zone in mapZones)
            {
                if (zone != null && !revealedZones.Contains(zone.id) && zone.Contains(point))
                    revealedZones.Add(zone.id);
            }
        }

        // Novo jogo ou checkpoint: repõe as zonas gravadas e, no Fácil, revela todas.
        void RestoreMap(string[] saved)
        {
            revealedZones.Clear();
            if (saved != null)
            {
                foreach (string zoneId in saved)
                {
                    if (mapZones.Any(z => z != null && z.id == zoneId))
                        revealedZones.Add(zoneId);
                }
            }
            if (FullMapFromStart)
            {
                foreach (var zone in mapZones.Where(z => z != null))
                    revealedZones.Add(zone.id);
            }
        }

        // Uma gravação pode não ter zonas (gravações anteriores ao mapa), mas não pode ter nomes vazios ou repetidos.
        static bool ValidRevealedZones(string[] zones)
        {
            return zones == null || (!zones.Any(string.IsNullOrWhiteSpace) && zones.Distinct().Count() == zones.Length);
        }

        // Mostra as imagens das zonas reveladas e põe o marcador do jogador no sítio certo.
        void RefreshMap()
        {
            foreach (var zone in mapZones)
            {
                if (zone != null && zone.mapImage != null)
                    zone.mapImage.SetActive(revealedZones.Contains(zone.id));
            }
            if (ui.mapArea == null || ui.mapPlayer == null)
                return;

            ui.mapPlayer.anchoredPosition = MapPoint(MapPosition);
            float yaw = InVan ? van.transform.eulerAngles.y : player.transform.eulerAngles.y;
            ui.mapPlayer.localEulerAngles = new Vector3(0, 0, -yaw);
        }

        // Converte uma posição do mundo num ponto da Área do mapa (a contar do canto inferior esquerdo).
        public Vector2 MapPoint(Vector3 world)
        {
            Vector2 size = ui.mapArea.rect.size;
            float x = Mathf.InverseLerp(mapWorldMin.x, mapWorldMax.x, world.x);
            float y = Mathf.InverseLerp(mapWorldMin.y, mapWorldMax.y, world.z);
            return new Vector2(x * size.x, y * size.y);
        }
    }
}
