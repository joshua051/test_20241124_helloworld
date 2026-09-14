"""Tests of the coordinator only. Fixtures are not Unity execution evidence."""
from pathlib import Path
import json
import subprocess
import sys
import tempfile
import unittest
import zipfile
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import run_unity_validation as v

class ValidationRunnerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.xml = self.root / "results.xml"
    def tearDown(self):
        self.temp.cleanup()
    def fixture(self, **attrs):
        values = dict(result="Passed", total="1", passed="1", failed="0", skipped="0", inconclusive="0")
        values.update(attrs)
        self.xml.write_text('<test-run ' + ' '.join(f'{k}="{x}"' for k,x in values.items()) + '><test-case fullname="IronSand.Tests.RequiredSuite.Test" result="Passed"/></test-run>')
    def test_desktop_cli_target_aliases(self):
        # Public coordinator/receipt enum names differ from Unity CLI aliases.
        self.assertEqual(v.BUILD_TARGET_ARGUMENTS, {
            "StandaloneWindows64": "win64", "StandaloneOSX": "osxuniversal",
            "StandaloneLinux64": "linux64"})
        self.assertEqual(v.TARGETS, set(v.BUILD_TARGET_ARGUMENTS))
    def test_complete_result_passes(self):
        self.fixture()
        self.assertEqual(v.read_test_result(self.xml, {"RequiredSuite"})["total"], 1)
    def test_missing_xml_rejected(self):
        with self.assertRaises(v.ValidationError): v.read_test_result(self.xml, set())
    def test_empty_xml_rejected(self):
        self.xml.touch()
        with self.assertRaises(v.ValidationError): v.read_test_result(self.xml, set())
    def test_bad_xml_rejected(self):
        self.xml.write_text("not xml")
        with self.assertRaises(v.ValidationError): v.read_test_result(self.xml, set())
    def test_zero_cases_rejected(self):
        self.fixture(total="0", passed="0")
        with self.assertRaises(v.ValidationError): v.read_test_result(self.xml, set())
    def test_failed_run_rejected(self):
        self.fixture(result="Failed", failed="1", passed="0")
        with self.assertRaises(v.ValidationError): v.read_test_result(self.xml, set())
    def test_skipped_case_rejected(self):
        self.fixture(skipped="1")
        with self.assertRaises(v.ValidationError): v.read_test_result(self.xml, set())
    def test_inconclusive_rejected(self):
        self.fixture(inconclusive="1")
        with self.assertRaises(v.ValidationError): v.read_test_result(self.xml, set())
    def test_count_mismatch_rejected(self):
        self.fixture(total="2", passed="2")
        with self.assertRaises(v.ValidationError): v.read_test_result(self.xml, set())
    def test_missing_suite_rejected(self):
        self.fixture()
        with self.assertRaises(v.ValidationError): v.read_test_result(self.xml, {"AbsentSuite"})
    def test_case_failure_cannot_hide_in_passed_run(self):
        self.fixture()
        self.xml.write_text(self.xml.read_text().replace('Test" result="Passed"', 'Test" result="Failed"'))
        with self.assertRaises(v.ValidationError): v.read_test_result(self.xml, set())
    def test_entity_xml_rejected(self):
        self.xml.write_text('<!DOCTYPE test-run [<!ENTITY x "value">]><test-run/>')
        with self.assertRaises(v.ValidationError): v.read_test_result(self.xml, set())
    def test_non_numeric_counts_rejected(self):
        self.fixture(total="NaN")
        with self.assertRaises(v.ValidationError): v.read_test_result(self.xml, set())
    def test_process_nonzero_rejected(self):
        with self.assertRaises(v.ValidationError):
            v.run_process([sys.executable, "-c", "raise SystemExit(9)"], self.root / "exit.log", 5)
    def test_process_timeout_rejected(self):
        with self.assertRaises(v.ValidationError):
            v.run_process([sys.executable, "-c", "import time; time.sleep(30)"], self.root / "timeout.log", .1)
    def test_safe_snapshot_extracts(self):
        z = self.root / "s.zip"
        with zipfile.ZipFile(z, "w") as archive: archive.writestr("Assets/test.txt", "source")
        v.extract_snapshot(z, self.root / "Project")
        self.assertEqual((self.root / "Project/Assets/test.txt").read_text(), "source")
    def test_snapshot_traversal_rejected(self):
        z = self.root / "s.zip"
        with zipfile.ZipFile(z, "w") as archive: archive.writestr("../outside", "bad")
        with self.assertRaises(v.ValidationError): v.extract_snapshot(z, self.root / "Project")
        self.assertFalse((self.root / "outside").exists())
    def test_snapshot_symlink_rejected(self):
        z = self.root / "s.zip"; info = zipfile.ZipInfo("link"); info.external_attr = 0o120777 << 16
        with zipfile.ZipFile(z, "w") as archive: archive.writestr(info, "outside")
        with self.assertRaises(v.ValidationError): v.extract_snapshot(z, self.root / "Project")
    def receipt(self):
        output = self.root / "Project/Builds/game"; output.parent.mkdir(parents=True); output.write_bytes(b"fixture")
        path = self.root / "receipt.json"
        path.write_text(json.dumps(dict(result="Succeeded", errors=0, unityVersion=v.EDITOR, target="StandaloneLinux64", output=str(output))))
        return path
    def test_receipt_requires_real_output_path(self):
        path = self.receipt()
        (self.root / "Project/Builds/game").unlink()
        with self.assertRaises(v.ValidationError): v.check_build_receipt(path, self.root / "Project", "StandaloneLinux64")
    def test_receipt_target_mismatch_rejected(self):
        path = self.receipt()
        with self.assertRaises(v.ValidationError): v.check_build_receipt(path, self.root / "Project", "StandaloneOSX")
    def test_valid_fixture_receipt_passes_only_build_stage(self):
        self.assertEqual(v.check_build_receipt(self.receipt(), self.root / "Project", "StandaloneLinux64")["boundary"], "Build only; not launched or soak-tested")
    def test_no_editor_reports_blocked_and_preserves_files(self):
        original = self.root / "keep.txt"; original.write_text("local edit")
        code = v.main(["--unity", str(self.root / "missing-Unity"), "--out", str(self.root / "evidence")])
        self.assertEqual(code, 2)
        report = json.loads(next((self.root / "evidence").glob("*/summary.json")).read_text())
        self.assertEqual(report["status"], "BLOCKED")
        self.assertEqual(report["stages"], {})
        self.assertEqual(original.read_text(), "local edit")
    def test_repeated_runs_do_not_reuse_old_results(self):
        for _ in range(2): v.main(["--unity", str(self.root / "missing"), "--out", str(self.root / "evidence")])
        self.assertEqual(len(list((self.root / "evidence").glob("*/summary.json"))), 2)

if __name__ == "__main__": unittest.main()
