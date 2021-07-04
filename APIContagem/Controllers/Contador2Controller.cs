using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using APIContagem.Models;
using Confluent.Kafka;

namespace APIContagem.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Contador2Controller : ControllerBase
    {
        private static readonly Contador _CONTADOR = new Contador();
        private readonly ILogger<Contador2Controller> _logger;
        private readonly IConfiguration _configuration;
        private static int _PARTITION = 0;

        public Contador2Controller(ILogger<Contador2Controller> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public ResultadoContador Get()
        {
            int valorAtualContador;
            int partition;

            lock (_CONTADOR)
            {
                if (_PARTITION == 9)
                    _PARTITION = 0;
                else
                    _PARTITION ++;
                
                _CONTADOR.Incrementar();

                valorAtualContador = _CONTADOR.ValorAtual;
                partition = _PARTITION;
            }


            var resultado = new ResultadoContador()
            {
                ValorAtual = valorAtualContador,
                Producer = _CONTADOR.Local,
                Kernel = _CONTADOR.Kernel,
                TargetFramework = _CONTADOR.TargetFramework,
                Mensagem = _configuration["MensagemVariavel"]
            };

            string topic = _configuration["ApacheKafka:Topic"];
            string jsonContagem = JsonSerializer.Serialize(resultado);

            var configKafka = new ProducerConfig
            {
                BootstrapServers = _configuration["ApacheKafka:Host"],
            };

            using (var producer = new ProducerBuilder<Null, string>(configKafka).Build())
            {
                var result = producer.ProduceAsync(
                    //topic,
                    new TopicPartition(topic, new Partition(partition)),
                    new Message<Null, string>
                    { Value = jsonContagem }).Result;

                _logger.LogInformation(
                    $"Apache Kafka - Envio para o tópico {topic} concluído | " +
                    $"{jsonContagem} | Status: { result.Status.ToString()}");
            }

            return resultado;
        }
    }
}