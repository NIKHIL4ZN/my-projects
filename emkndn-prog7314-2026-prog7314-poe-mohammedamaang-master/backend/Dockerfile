# Optional: containerises the backend for deployment (e.g. Azure App Service
# for Containers), matching the hosting plan in the Part 1 design (Section 5.3).
# Not required to run the API locally - see README Step 5 for that.

FROM eclipse-temurin:17-jdk AS build
WORKDIR /app
COPY . .
RUN chmod +x ./gradlew
RUN ./gradlew installDist --no-daemon

FROM eclipse-temurin:17-jre
WORKDIR /app
COPY --from=build /app/build/install/familylink-api /app
EXPOSE 8080
ENTRYPOINT ["/app/bin/familylink-api"]
