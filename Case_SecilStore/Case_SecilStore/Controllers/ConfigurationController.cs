using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SecilStore_ConfigLibrary.Interfaces;
using SecilStore_ConfigLibrary.Models;
using SecilStore_ConfigLibrary.Repositories;
using MongoDB.Bson;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Case_SecilStore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationRepository _repository;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(IConfigurationRepository repository, ILogger<ConfigurationController> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConfigurationEntry>>> GetAll([FromQuery] string applicationName)
        {
            try
            {
                if (string.IsNullOrEmpty(applicationName))
                    return BadRequest("Application name is required");

                var configurations = await _repository.GetAllConfigurationsAsync(applicationName);
                return Ok(configurations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configurations");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ConfigurationEntry>> Get(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return BadRequest("Id is required");

                var configuration = await _repository.GetConfigurationByIdAsync(id);
                if (configuration == null)
                    return NotFound();

                return Ok(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration by id");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("bykey")]
        public async Task<ActionResult<ConfigurationEntry>> GetByKey([FromQuery] string applicationName, [FromQuery] string key)
        {
            try
            {
                if (string.IsNullOrEmpty(applicationName) || string.IsNullOrEmpty(key))
                    return BadRequest("Application name and key are required");

                var configuration = await _repository.GetConfigurationAsync(applicationName, key);

                if (configuration == null)
                    return NotFound();

                return Ok(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration by key");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ConfigurationEntry>> Create([FromBody] ConfigurationEntry configuration)
        {
            try
            {
                if (configuration == null)
                    return BadRequest("Configuration is required");

                if (string.IsNullOrEmpty(configuration.ApplicationName) || string.IsNullOrEmpty(configuration.Name))
                    return BadRequest("Application name and name are required");

                var existingConfiguration = await _repository.GetConfigurationAsync(configuration.ApplicationName, configuration.Name);
                if (existingConfiguration != null)
                    return Conflict("Configuration with the same name already exists");

                // MongoDB ObjectId için otomatik Id oluşturulacak
                configuration.Id = ObjectId.GenerateNewId().ToString();
                configuration.CreatedAt = DateTime.UtcNow;
                configuration.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Creating configuration: {ApplicationName} - {Name}", configuration.ApplicationName, configuration.Name);
                var createdConfiguration = await _repository.AddConfigurationAsync(configuration);
                return CreatedAtAction(nameof(Get), new { id = createdConfiguration.Id }, createdConfiguration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating configuration");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ConfigurationEntry>> Update(string id, [FromBody] ConfigurationEntry configuration)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || configuration == null)
                    return BadRequest("Id and configuration are required");

                if (string.IsNullOrEmpty(configuration.Id))
                {
                    configuration.Id = id;
                }
                else if (id != configuration.Id)
                {
                    return BadRequest("Id mismatch");
                }

                var existingConfiguration = await _repository.GetConfigurationByIdAsync(id);
                if (existingConfiguration == null)
                    return NotFound();

                // Güncelleme zamanını ayarla
                configuration.UpdatedAt = DateTime.UtcNow;
                // Oluşturulma zamanını koruyalım
                configuration.CreatedAt = existingConfiguration.CreatedAt;

                _logger.LogInformation("Updating configuration: {Id} - {ApplicationName} - {Name}", id, configuration.ApplicationName, configuration.Name);
                var updatedConfiguration = await _repository.UpdateConfigurationAsync(configuration);
                return Ok(updatedConfiguration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return BadRequest("Id is required");

                _logger.LogInformation("Deleting configuration: {Id}", id);
                var result = await _repository.DeleteConfigurationAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting configuration");
                return StatusCode(500, "Internal server error");
            }
        }
    }
} 