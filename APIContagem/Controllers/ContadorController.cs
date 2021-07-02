using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using APIContagem.Models;
using MassTransit.KafkaIntegration;

namespace APIContagem.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContadorController : ControllerBase
    {
        private static readonly Contador _CONTADOR = new Contador();
        private readonly ILogger<ContadorController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ITopicProducer<IResultadoContador> _producer;

        public ContadorController(ILogger<ContadorController> logger,
            IConfiguration configuration,
            ITopicProducer<IResultadoContador> producer)
        {
            _logger = logger;
            _configuration = configuration;
            _producer = producer;
        }

        [HttpGet]
        public ResultadoContador Get()
        {
            int valorAtualContador;
            lock (_CONTADOR)
            {
                _CONTADOR.Incrementar();
                valorAtualContador = _CONTADOR.ValorAtual;
            }

            _logger.LogInformation($"Contador - Valor atual: {valorAtualContador}");

            var resultado = new ResultadoContador()
            {
                ValorAtual = valorAtualContador,
                Producer = _CONTADOR.Local,
                Kernel = _CONTADOR.Kernel,
                TargetFramework = _CONTADOR.TargetFramework,
                Mensagem = _configuration["MensagemVariavel"]
            };

            _producer.Produce(resultado);
            
            return resultado;
        }
    }
}