import json
import yaml

deploy = False


def setup():
    # Grab the parameters file
    with open(".circleci/scripts/parameters.json", "r") as f:
        global params
        params = json.load(f)

    # Grab the template configuration
    with open(".circleci/scripts/config-template.yml", "r") as yf:
        global config
        config = yaml.safe_load(yf)

    # Grab available connector jobs
    with open(".circleci/scripts/connector-jobs.yml", "r") as cf:
        global connector_jobs
        connector_jobs = yaml.safe_load(cf)

    with open(".circleci/scripts/common-jobs.yml", "r") as cf:
        global common_jobs
        common_jobs = yaml.safe_load(cf)


def getTagFilter(connector_name):
    version_regex = "([0-9]+)\\.([0-9]+)\\.([0-9]+)(?:-\\w+)?$"
    return f"/^({connector_name}|all)\\/{version_regex}/"


print("---- Started creating config ----")

setup()

# Get the main workflow
main_workflow = config["workflows"]["build"]

build_core = False
if "core" in params.keys():
    build_core = params["core"]

for connector, run in params.items():
    # Add any common jobs first
    if connector in common_jobs.keys():
        common_jobs_to_run = common_jobs[connector]
        main_workflow["jobs"] += common_jobs_to_run
        print(f"Added common jobs: {connector}")

    # Add connector jobs
    if run and connector in connector_jobs.keys():
        # Get jobs to run per connector
        jobs_to_run = connector_jobs[connector]
        # Setup each job
        for j in jobs_to_run:
            # Job only has one key with the job name
            job_name = next(iter(j.keys()))
            jobAttrs = j[job_name]

            # Add common requirements only to connector jobs.
            is_connector_job = job_name.find("build-connector") >= 0
            if is_connector_job:
                # Get the slug
                slug = connector if "slug" not in jobAttrs.keys() else jobAttrs["slug"]
                # Make sure you've initialized the 'requires' item
                if "requires" not in jobAttrs.keys():
                    jobAttrs["requires"] = []
                # Require objects to build for all connectors
                jobAttrs["requires"] += ["build-objects"]
                if build_core:
                    # Require core tests too if core needs rebuilding.
                    jobAttrs["requires"] += ["test-core"]
                # Add name to all jobs
                jobAttrs["name"] = f"{slug}-build"

            # Add tags if marked for deployment
            if deploy:
                jobAttrs["installer"] = True
                jobAttrs["filters"] = {
                    "branches": {"ignore": "/.*/"},
                    "tags": {"only": getTagFilter(connector)},
                }

        # Append connector jobs to main workflow jobs
        main_workflow["jobs"] += connector_jobs[connector]
        print(f"Added connector jobs: {connector}")

# Output continuation file
with open(".circleci/continuation-config.yml", "w") as file:
    documents = yaml.dump(config, file, sort_keys=False)

print("---- Finished creating config ----")
