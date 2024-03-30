using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SharpCompress.Common;
using System.Linq.Expressions;
using TaskManager.Context;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly IMongoCollection<TaskModel> _collection;
        private readonly IProducer<string, string> _kafkaProducer;

        public TasksController(TaskDbContext context, IProducer<string, string> kafkaProducer)
        {
            _collection = context.Database.GetCollection<TaskModel>(typeof(TaskModel).Name);
            _kafkaProducer = kafkaProducer;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskModel>>> GetTasks()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskModel>> GetTask(int id)
        {
            Expression<Func<TaskModel, bool>> filterExpression = entity => entity.Id == id;
            var task = await _collection.Find(filterExpression).FirstOrDefaultAsync();
          
            if (task == null)
            {
                return NotFound();
            }

            return task;
        }

        [HttpPost]
        public async Task<ActionResult<TaskModel>> CreateTask(TaskModel task)
        {
            await _collection.InsertOneAsync(task);

            await _kafkaProducer.ProduceAsync("task-created", new Message<string, string>
            {
                Key = task.Id.ToString(),
                Value = $"Task '{task.Title}' created."
            });

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, TaskModel task)
        {
            if (id != task.Id)
            {
                return BadRequest();
            }

            await _collection.ReplaceOneAsync(Builders<TaskModel>.Filter.Eq("_id", id), task);

            await _kafkaProducer.ProduceAsync("task-updated", new Message<string, string>
            {
                Key = task.Id.ToString(),
                Value = $"Task '{task.Title}' updated."
            });

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            await _collection.DeleteOneAsync(Builders<TaskModel>.Filter.Eq("_id", id));

            Expression<Func<TaskModel, bool>> filterExpression = entity => entity.Id == id;
            var task = await _collection.Find(filterExpression).FirstOrDefaultAsync();

            if (task == null)
            {
                return NotFound();
            }

            await _collection.DeleteOneAsync(Builders<TaskModel>.Filter.Eq("_id", id));

            await _kafkaProducer.ProduceAsync("task-deleted", new Message<string, string>
            {
                Key = task.Id.ToString(),
                Value = $"Task '{task.Title}' deleted."
            });

            return NoContent();
        }
    }
}
