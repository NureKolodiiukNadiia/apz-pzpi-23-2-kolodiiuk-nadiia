@Service
public class PlaybackEventService
{
    private final KafkaTemplate<String, PlaybackEvent> kafkaTemplate;

    public PlaybackEventService(KafkaTemplate<String, PlaybackEvent> kafkaTemplate) {
        this.kafkaTemplate = kafkaTemplate;
    }

    public CompletableFuture<SendResult<String, PlaybackEvent>> publishPlaybackEvent(PlaybackEvent event) {

        ProducerRecord<String, PlaybackEvent> record =
            new ProducerRecord<>("playback-events", event.getUserId(), event);

        record.headers().add("event-type", "PLAYBACK_STARTED".getBytes());
        record.headers().add("schema-version", "v1".getBytes());

        return kafkaTemplate.send(record)
            .whenComplete((result, ex) -> {
            if (ex != null) {
                handleFailure(event, ex);
            } else {
                logSuccess(result);
            }
        });
    }

    private void handleFailure(PlaybackEvent event, Throwable ex) {
        // обробка помилки або повторна відправка
    }

    private void logSuccess(SendResult<String, PlaybackEvent> result) {
        // логування та метрики
    }
}

spring:
kafka:
producer:
acks: all
retries: 10
enable-idempotence: true
key-serializer: org.apache.kafka.common.serialization.StringSerializer
value-serializer: io.confluent.kafka.serializers.KafkaAvroSerializer 

@Component
public class PlaybackEventConsumer
{
    @KafkaListener(
        topics = "playback-events",
        groupId = "analytics-service"
    )
    public void consume(ConsumerRecord<String, PlaybackEvent> record,
        Acknowledgment ack) {

        try {
            PlaybackEvent event = record.value();

            process(event);

            ack.acknowledge();

        } catch (Exception ex) {
            handleFailure(record, ex);
        }
    }

    private void process(PlaybackEvent event) {
        // бізнес-логіка обробки
    }

    private void handleFailure(ConsumerRecord<String, PlaybackEvent> record, Exception ex) {
        // відправка у DLQ
    }
}


pipeline
    .readFromKafka("playback-events")
    .map(parseEvent)
    .filter(_.duration > 30)
    .keyBy(_.userId)
    .windowBy(FixedWindows(Duration.standardDays(1)))
    .aggregate(
        zero = UserStats.empty,
        seqOp = (acc, e) => acc.add(e),
        combOp = (a, b) => a.merge(b)
    )
    .map(toBigQueryRow)
    .writeToBigQuery("analytics.daily_user_stats")

private void handleFailure(ConsumerRecord<String, PlaybackEvent> record, Exception ex) { 
    kafkaTemplate.send("playback-events-dlq", record.key(), record.value());
}


@KafkaListener(topics = "playback-events", groupId = "recommendation-service")
public void handle(PlaybackEvent event) {
       updateUserEmbedding(event);
       updateRecommendations(event.getUserId());
}
