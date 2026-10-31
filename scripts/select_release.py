#!/usr/bin/env python3
"""Select a shared release for All; retain optional component-tag releases."""
import json
import os
import re
from prepare_release import PROJECTS

def select(event, ref, ref_type, sha, requested='All'):
    if not re.fullmatch(r'[0-9a-f]{40}', sha):
        raise ValueError('Release needs the exact checked-out commit SHA.')
    tagged = event == 'release' or event == 'push' and ref_type == 'tag'
    component = next((p for p in PROJECTS if tagged and ref.startswith(p.lower()+'-v')), None)
    if component:
        plugins = [component]
    elif tagged:
        plugins = list(PROJECTS)
    elif requested == 'All':
        plugins = list(PROJECTS)
    elif requested in PROJECTS:
        plugins = [requested]
    else:
        raise ValueError('Unknown plugin selection.')
    bundle = len(plugins) > 1
    # A source commit has one stable bundle tag, so retries resume its draft.
    tag = ref if tagged else 'pb-'+sha[:12] if bundle else ''
    if tag and not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9._-]{0,199}', tag):
        raise ValueError('Release tag contains unsupported characters.')
    return {'plugins': json.dumps(plugins), 'bundle': str(bundle).lower(),
            'tag': tag, 'existing_release': str(event == 'release').lower()}

if __name__ == '__main__':
    result = select(os.environ['GITHUB_EVENT_NAME'], os.environ['GITHUB_REF_NAME'],
                    os.environ.get('GITHUB_REF_TYPE', ''), os.environ['GITHUB_SHA'],
                    os.environ.get('REQUESTED_PLUGIN', 'All'))
    with open(os.environ['GITHUB_OUTPUT'], 'a') as output:
        output.writelines(f'{key}={value}\n' for key, value in result.items())
