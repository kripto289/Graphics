from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import projectcontext_filepath, projectcontext_job_id_test_all, projectcontext_job_id_test, projectcontext_job_id_test_min_editor
from ..shared.yml_job import YMLJob
from ..shared.constants import NPM_UPMCI_INSTALL_URL


class Project_AllPackageCiJob():
    
    def __init__(self, packages, agent, platforms, target_branch, editor):
        self.job_id = projectcontext_job_id_test_all(editor["name"])
        self.yml = self.get_job_definition(packages, agent, platforms, target_branch, editor).get_yml()


    def get_job_definition(self, packages, agent, platforms, target_branch, editor):

        # define dependencies
        dependencies = []
        for platform in platforms:
            dependencies.append(f'{projectcontext_filepath()}#{projectcontext_job_id_test(platform["os"],editor["name"])}')
            if str(editor["track"]).lower() == "trunk":
                dependencies.append(f'{projectcontext_filepath()}#{projectcontext_job_id_test_min_editor(platform["os"])}')
                #dependencies.append(f'{packages_filepath()}#{package_job_id_test_dependencies(package["id"],platform["os"],editor["track"])}')
        
        # construct job
        job = YMLJob()
        job.set_name(f'Pack and test all packages - { editor["name"]} [project context]')
        job.set_agent(agent)
        job.add_dependencies(dependencies)
        job.add_var_custom_revision(editor["track"])
        if editor["track"] != "CUSTOM-REVISION":
            job.set_trigger_on_expression(f'pull_request.target eq "{target_branch}" AND NOT pull_request.draft')
        job.add_commands([
                f'npm install upm-ci-utils@stable -g --registry {NPM_UPMCI_INSTALL_URL}',
                f'upm-ci package izon -t',
                f'upm-ci package izon -d'])
        return job
        
    