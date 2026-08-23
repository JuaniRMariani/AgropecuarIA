import sys

path = r'B:\Xenova\AgropecuarIA\contracts\identity.openapi.yaml'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

new_paths = '''
  /api/identity/mfa/totp/setup:
    post:
      summary: Setup TOTP
      security:
        - SessionCookie: []
        - CsrfToken: []
      responses:
        '200':
          description: OK
        '400': { $ref: '#/components/responses/BadRequest' }
        '401': { $ref: '#/components/responses/Unauthorized' }
        '409': { $ref: '#/components/responses/Conflict' }

  /api/identity/mfa/totp/enable:
    post:
      summary: Enable TOTP
      security:
        - SessionCookie: []
        - CsrfToken: []
      parameters:
        - name: unverifiedSecret
          in: query
          required: true
          schema:
            type: string
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
      responses:
        '200':
          description: OK
        '400': { $ref: '#/components/responses/BadRequest' }
        '401': { $ref: '#/components/responses/Unauthorized' }
        '409': { $ref: '#/components/responses/Conflict' }

  /api/identity/mfa/totp/disable:
    post:
      summary: Disable TOTP
      security:
        - SessionCookie: []
        - CsrfToken: []
      responses:
        '204':
          description: No Content
        '400': { $ref: '#/components/responses/BadRequest' }
        '401': { $ref: '#/components/responses/Unauthorized' }
        '409': { $ref: '#/components/responses/Conflict' }

  /api/identity/mfa/recovery/consume:
    post:
      summary: Consume Recovery Code
      security:
        - SessionCookie: []
        - CsrfToken: []
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
      responses:
        '204':
          description: No Content
        '400': { $ref: '#/components/responses/BadRequest' }
        '401': { $ref: '#/components/responses/Unauthorized' }
'''

# insert before components
content = content.replace('components:\n', new_paths + '\ncomponents:\n')

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
