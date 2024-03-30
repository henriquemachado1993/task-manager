using Confluent.Kafka;
using TaskManager.Context;
using TaskManager.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Config access mongoDB
var mongoDbConfig = builder.Configuration.GetSection("MongoDbConfig").Get<MongoDbConfig>();
builder.Services.AddSingleton(mongoDbConfig);

builder.Services.AddScoped<TaskDbContext>();

var producerKafkaConfig = builder.Configuration.GetSection("KafkaConfig").Get<ProducerConfig>();

builder.Services.AddSingleton<ProducerConfig>(config =>
{
    return producerKafkaConfig ?? new ProducerConfig();
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var producerConfig = sp.GetRequiredService<ProducerConfig>();
    return new ProducerBuilder<string, string>(producerConfig).Build();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
