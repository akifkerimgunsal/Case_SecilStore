using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecilStore_ConfigLibrary.Interfaces;
using SecilStore_ConfigLibrary.Models;
using SecilStore_ConfigLibrary.Repositories;
using System.Linq;

namespace Case_SecilStore.Pages.Configuration
{
    public class IndexModel : PageModel
    {
        private readonly IConfigurationRepository _repository;

        public IndexModel(IConfigurationRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public IEnumerable<ConfigurationEntry> Configurations { get; private set; }
        public IEnumerable<string> Applications { get; private set; }

        public async Task OnGetAsync()
        {
            var allConfigurations = await _repository.GetAllConfigurationsFromAllApplicationsAsync();
            
            // Konfigürasyonları Name değerine göre A'dan Z'ye sırala
            Configurations = allConfigurations.OrderBy(c => c.Name);
            
            // Benzersiz uygulama adlarını al
            Applications = Configurations.Select(c => c.ApplicationName).Distinct().OrderBy(a => a);
        }
    }
} 