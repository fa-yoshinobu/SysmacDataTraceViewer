import contextlib
import io
import importlib.util
from pathlib import Path
import tempfile
import unittest

SPEC = importlib.util.spec_from_file_location("check_runtime", Path(__file__).resolve().parents[2] / ".github/scripts/check_runtime.py")
runtime = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(runtime)


class RuntimeCheckTests(unittest.TestCase):
    def test_detects_older_runtime_but_does_not_request_downgrades(self):
        metadata = {"latest-runtime": "10.0.15"}
        with contextlib.redirect_stdout(io.StringIO()):
            self.assertFalse(runtime.check_version("10.0.14", metadata))
        self.assertTrue(runtime.check_version("10.0.15", metadata))
        self.assertTrue(runtime.check_version("10.0.16", metadata))

    def test_rejects_wrong_channel_and_prerelease_metadata(self):
        for latest in ["11.0.1", "10.0.16-preview.1"]:
            with self.assertRaises(ValueError):
                runtime.check_version("10.0.15", {"latest-runtime": latest})

    def test_reads_project_pin_and_rejects_missing_pin(self):
        with tempfile.TemporaryDirectory() as directory:
            project = Path(directory) / "app.csproj"
            project.write_text("<Project><PropertyGroup><RuntimeFrameworkVersion>10.0.15</RuntimeFrameworkVersion></PropertyGroup></Project>", encoding="utf-8")
            self.assertEqual("10.0.15", runtime.runtime_version(project))
            project.write_text("<Project />", encoding="utf-8")
            with self.assertRaises(ValueError):
                runtime.runtime_version(project)
