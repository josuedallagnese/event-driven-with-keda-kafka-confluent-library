<p style="font-size: 25px" align="center"><b>Event Driven Autoscaling + Keda + Kafka + Confluent Library</b></p>

1) **Pré-requisitos**
* Criar o cluster de Kafka:
    - https://github.com/josuedallagnese/kafka-cluster-on-aks

    - Criar os topicos que serão usados nesse exemplo:
    ```
    kubectl apply -f strimzi/topic-keda-worker.yaml -n ingestion
    kubectl apply -f strimzi/topic-reply-keda-worker.yaml -n ingestion
    ```
2) **Installar o keda em seu cluster de kubernetes como achar melhor:**

https://keda.sh/docs/2.6/deploy/

3) **Criar o namespace "worker"**:
    - kubectl create namespace worker

4) **Aplique contra o seu ambiente de kubernetes**.
    - kubectl apply -f deployment.yaml

**OBS**: se preferir modifique os valores de pollingInterval, cooldownPeriod. Eles foram configurados com intervalos menores para visualizar o Autoscaling acontecendo.

5) **Abra a solução Keda.sln e configure em todos os arquivos de configuração e os parametros para rodar local:**
    - BootstrapServers: **Bootstrap servers do Kafka**

6) **Rode o projeto producer** escolhe a opção 1 e acompanhe pelo kubernetes o deployment fazer o autoscaling automatico.

7) **Para limpar todos os recursos do teste:**
    - kubectl delete namespace worker
