import json
from re import S
from typing import Any, Dict, List
import yaml
import sys
import getopt


def runCommand(argv: List[str]):
    deploy = False
    output_filepath = ".circleci/continuation-config.yml"
    arg_help = "{0} -d <deploy?> -o <output>".format(argv[0])

    print(argv)
    try:
        opts, _ = getopt.getopt(argv[1:], "hd:o:")
    except:
        print(arg_help)
        sys.exit(2)

    for opt, arg in opts:
        if opt in ("-h", "--help"):
            print(arg_help)  # print the help message
            sys.exit(2)
        elif opt in ("-d", "--deploy"):
            deploy = arg is not None and arg not in [
                "none",
                "None",
                "False",
                "false",
                "f",
            ]
            print("deploy arg -- " + str(arg) + " -- " + str(deploy))
        elif opt in ("-o", "--output"):
            output_filepath = arg

    createConfigFile(deploy, output_filepath)


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


def getTagRegexString(connector_names: List[str]):
    # version_regex = "([0-9]+)\\.([0-9]+)\\.([0-9]+)(?:-\\w+)?"
    # tags = connector_names + ["all"]
    # tagExpression = "(" + "|".join(tags) + ")"
    # return f"/^{version_regex}\\/{tagExpression}$/"

    # Version format 'x.y.z' with optional suffix '-{SUFFIX_NAME}' and optional '/all' ending to force build all tags
    return "/^([0-9]+)\\.([0-9]+)\\.([0-9]+)(?:-\\w+)?(\\/all)?$/"


def getTagFilter(connector_names: List[str]):
    return {
        "branches": {"ignore": "/.*/"},
        "tags": {"only": getTagRegexString(connector_names)},
    }


def createConfigFile(deploy: bool, outputPath: str):
    print("---- Started creating config ----")
    print(
        f"\n  -- Settings --\n  Deploy: {deploy}\n  Output path: {outputPath}\n  --\n"
    )
    setup()

    # Get the main workflow
    main_workflow = config["workflows"]["build"]

    build_core = False
    if "core" in params.keys():
        build_core = params["core"]

    jobs_before_deploy: List[str] = []
    slugs_to_match: List[str] = []
    for connector, run in params.items():
        # Add any common jobs first
        if connector in common_jobs.keys():
            common_jobs_to_run = common_jobs[connector]
            main_workflow["jobs"] += common_jobs_to_run
            print(f"Added common jobs: {connector}")

        # Add connector jobs
        if run and connector in connector_jobs.keys():
            print(f"Started adding connector jobs: {connector}")

            # Get jobs to run per connector
            jobs_to_run = connector_jobs[connector]
            if connector not in slugs_to_match:
                slugs_to_match.append(connector)
            # Setup each job
            for j in jobs_to_run:
                # Job only has one key with the job name
                job_name = next(iter(j.keys()))
                jobAttrs = j[job_name]

                # Add common requirements only to connector jobs.
                is_connector_job = job_name.find("build-connector") >= 0
                if is_connector_job:
                    # Get the slug
                    slug = (
                        connector if "slug" not in jobAttrs.keys() else jobAttrs["slug"]
                    )

                    # Make sure you've initialized the 'requires' item
                    if "requires" not in jobAttrs.keys():
                        jobAttrs["requires"] = []
                    # Require objects to build for all connectors
                    jobAttrs["requires"] += ["build-objects"]
                    if build_core:
                        # Require core tests too if core needs rebuilding.
                        jobAttrs["requires"] += ["test-core"]
                    # Add name to all jobs
                    name = f"{slug}-build"
                    if "name" not in jobAttrs.keys():
                        jobAttrs["name"] = name
                    n = jobAttrs["name"]
                    jobs_before_deploy.append(n)
                    print(f"    Added connector job: {n}")
                    # Add tags if marked for deployment
                    if deploy:
                        jobAttrs["installer"] = True
                if deploy:
                    jobAttrs["filters"] = getTagFilter([connector])

            # Append connector jobs to main workflow jobs
            main_workflow["jobs"] += connector_jobs[connector]

    # Modify jobs for deployment
    if deploy:
        deploy_job = {}
        deploy_job["filters"] = getTagFilter(slugs_to_match)
        deploy_job["requires"] = jobs_before_deploy
        main_workflow["jobs"] += [{"deploy-connectors": deploy_job}]
        print("Added deploy job: deployment")

        if "get-ci-tools" in main_workflow["jobs"]:
            main_workflow["jobs"].remove("get-ci-tools")

        ci_tools_job = {"filters": getTagFilter(slugs_to_match)}
        main_workflow["jobs"] += [{"get-ci-tools": ci_tools_job}]
        print("Modified job for deploy: get-ci-tools")

        for job in main_workflow["jobs"]:
            x = list(job.keys())
            jobAttrs = job[x[0]]
            if "filters" not in jobAttrs.keys():
                jobAttrs["filters"] = getTagFilter(slugs_to_match)
                print(f"Added missing filter to job: {x[0]}")

        for jobName in jobs_before_deploy:
            main_workflow["jobs"] += [getNewDeployJob(jobName)]
    # Output continuation file
    with open(outputPath, "w") as file:
        yaml.dump(config, file, sort_keys=False)

    print("---- Finished creating config ----")


def getNewDeployJob(jobName: str):
    slug = jobName.split("-build")[0]
    isMac = jobName.find("-mac") != -1
    deployJob: Dict[str, Any] = {
        "slug": slug.split("-mac")[0] if isMac else slug,
        "name": slug + "-deploy-mac" if isMac else slug + "-deploy" ,
        "os": "OSX" if isMac else "Win",
        "arch": "Any",
        "extension": "zip" if isMac else "exe",
        "requires": ["deploy-connectors", jobName],
        "filters": getTagFilter([jobName]),
    }
    return {"deploy-connector-new": deployJob}


if __name__ == "__main__":
    runCommand(sys.argv)
