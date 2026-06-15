// @author: Gilles Lavoie - Architecte principal - 2024-2026
using GisyWeb.Infrastructure.AdminDb;
using GisyWeb.Infrastructure.AdminDb.Entities;
using GisyWeb.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GisyWeb.Infrastructure.Services
{
    public class UiOrchestratorService
    {
        private readonly IDbContextFactory<Carto5AdminContext> _dbFactory;
        private readonly SessionContext _session;

        // Cache mémoire pour éviter les accès BD répétitifs durant la session utilisateur
        private Dictionary<string, FeatureState> _cache = new(StringComparer.OrdinalIgnoreCase);

        public UiOrchestratorService(IDbContextFactory<Carto5AdminContext> dbFactory, SessionContext session)
        {
            _dbFactory = dbFactory;
            _session = session;
        }

        // =================================================================================
        // 1. MOTEUR D'AFFICHAGE (UTILISÉ PAR L'APPLICATION BLAZOR EN COURS D'EXÉCUTION)
        // =================================================================================

        /// <summary>
        /// Charge la configuration UI du client actuel depuis CARTO5_CLIENT_SETTING.
        /// Appelée au démarrage du MainLayout.
        /// </summary>
        public async Task LoadConfigAsync()
        {
            using var ctx = await _dbFactory.CreateDbContextAsync();

            // On récupère uniquement le scope UI_FEATURE pour le client en cours (CurrentModule)
            var settings = await ctx.ClientSettings
                .AsNoTracking()
                .Where(s => s.ClientCode == _session.CurrentModule && s.Scope == "UI_FEATURE")
                .ToListAsync();

            // Transformation en cache rapide
            _cache = settings.ToDictionary(
                s => s.SettingKey,
                s => new FeatureState
                {
                    // Si la valeur est "0", la feature est désactivée. Sinon elle est active.
                    IsActive = s.SettingValue != "0",
                    // Si c'est juste "1", on n'a pas d'options spécifiques. Sinon, c'est du JSON ou du texte.
                    Options = (s.SettingValue == "1" || s.SettingValue == "0") ? null : s.SettingValue
                });
        }

        /// <summary>
        /// Vérifie si un composant (bouton, widget) est autorisé à s'afficher pour le client actuel.
        /// </summary>
        public bool IsActive(string featureCode)
        {
            return _cache.TryGetValue(featureCode, out var state) && state.IsActive;
        }

        /// <summary>
        /// Récupère la configuration spécifique (JSON ou Classe CSS) pour "habiller" l'outil.
        /// </summary>
        public string? GetOption(string featureCode)
        {
            return _cache.TryGetValue(featureCode, out var state) ? state.Options : null;
        }

        private class FeatureState
        {
            public bool IsActive { get; set; }
            public string? Options { get; set; }
        }


        // =================================================================================
        // 2. MOTEUR D'AdminISTRATION (UTILISÉ PAR LE "MAGASIN DE COMPOSANTS" M.O.C.)
        // =================================================================================

        /// <summary>
        /// Retourne le catalogue complet des outils disponibles dans Carto5 (Table CARTO5_FEATURE_REGISTRY).
        /// </summary>
        public async Task<List<FeatureRegistry>> GetAllFeaturesAsync()
        {
            using var ctx = await _dbFactory.CreateDbContextAsync();
            return await ctx.FeatureRegistries
                .AsNoTracking()
                .OrderBy(f => f.Label)
                .ToListAsync();
        }

        /// <summary>
        /// Retourne la configuration actuelle d'un client spécifique (Table CARTO5_CLIENT_SETTING).
        /// Convertit le format Setting en DTO pour l'interface d'Administration.
        /// </summary>
        public async Task<List<CartoClientSetting>> GetAllConfigsForClientAsync(string clientCode)
        {
            using var ctx = await _dbFactory.CreateDbContextAsync();
            return await ctx.ClientSettings
                .AsNoTracking()
                .Where(s => s.ClientCode == clientCode && s.Scope == "UI_FEATURE")
                .ToListAsync();
        }

        /// <summary>
        /// Enregistre les modifications faites dans l'écran d'Administration pour un client donné.
        /// Dégage les vieux réglages UI et insère les nouveaux.
        /// </summary>
        public async Task SaveClientFeaturesAsync(string clientCode, List<CartoClientSetting> newSettings)
        {
            using var ctx = await _dbFactory.CreateDbContextAsync();

            // 1. Suppression de l'ancienne configuration UI pour ce client
            var oldSettings = await ctx.ClientSettings
                .Where(s => s.ClientCode == clientCode && s.Scope == "UI_FEATURE")
                .ToListAsync();

            if (oldSettings.Any())
            {
                ctx.ClientSettings.RemoveRange(oldSettings);
            }

            // 2. Ajout de la nouvelle configuration (uniquement ce qui est actif)
            // On s'assure que le Scope est bien forcé à "UI_FEATURE"
            var validSettings = newSettings.Where(s => !string.IsNullOrEmpty(s.SettingKey)).ToList();

            foreach (var setting in validSettings)
            {
                setting.ClientCode = clientCode;
                setting.Scope = "UI_FEATURE";
                setting.CodModOri = "GLO"; // Standard Carto5
                setting.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                setting.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Si la valeur est vide mais qu'on sauvegarde, on met "1" par défaut pour "Actif"
                if (string.IsNullOrWhiteSpace(setting.SettingValue))
                {
                    setting.SettingValue = "1";
                }
            }

            if (validSettings.Any())
            {
                ctx.ClientSettings.AddRange(validSettings);
            }

            // 3. Commit en base
            await ctx.SaveChangesAsync();
        }
    }
}