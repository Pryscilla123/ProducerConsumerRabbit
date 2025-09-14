using Microsoft.AspNetCore.Mvc;
using Producer.Api.Infrastructure;
using Producer.Api.Models;

namespace Producer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArmazemController : ControllerBase
    {
        private readonly RabbitMqProducer _producer; 
        public ArmazemController(RabbitMqProducer producer)
        {
            _producer = producer;
        }

        [HttpPost]
        public IActionResult Post([FromBody] ArmazemViewModel armazem)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _producer.SendMessage(armazem);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Message = "Internal Server Error.", Details = ex.Message });
                }
            }
            // Lógica para processar o valor recebido
            return Ok(armazem);
        }
    }
}
