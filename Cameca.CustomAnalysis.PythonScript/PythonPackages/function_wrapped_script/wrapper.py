import io
import textwrap
import tokenize
import functools


FUNC_WRAP_TEMPLATE = """
from function_wrapped_script.wrapper import suppress_keyboardinterrupt
@suppress_keyboardinterrupt
{signature}:
{indented}
""".lstrip()


class ReindentTransformer:

    def __init__(self, indent='\t'):
        self.indent = indent
        self.level = 0

    def transform(self, text):
        stream = io.StringIO(text)
        token_generator = tokenize.generate_tokens(stream.readline)
        transformed_tokens = map(self._transform_token, token_generator)
        try:
            return (True, tokenize.untokenize(transformed_tokens))
        except tokenize.TokenError:
            return (False, text)

    def _transform_token(self, token):
        if token.type == tokenize.INDENT:
            self.level += 1
        if token.type == tokenize.DEDENT:
            self.level -= 1
        if token.type == tokenize.INDENT:
            return (tokenize.INDENT, self.indent * self.level)
        else:
            return (token.type, token.string)


def suppress_keyboardinterrupt(func):
    @functools.wraps(func)
    def inner(*args, **kwargs):
        try:
            _run_results = func(*args, **kwargs)
        except KeyboardInterrupt:
            pass
        return locals()
    return inner


def transform_and_indent(script, indent='\t'):
    if script is None or script.strip() == '':
        script = 'pass'
    transformer = ReindentTransformer(indent)
    success, transformed = transformer.transform(script)
    if not success:
        return transformed
    return textwrap.indent(transformed, indent)


def function_wrapper(script, func_signature, indent='\t'):
    clean_sig = func_signature.strip().rstrip(':')
    indented = transform_and_indent(script, indent)
    return FUNC_WRAP_TEMPLATE.format(**{
        'signature': clean_sig,
        'indented': indented,
    })