import unittest

from pipeline import recent_subjects, predicted_center


class TrackingContinuityTests(unittest.TestCase):
    def test_brief_missing_frame_retains_candidates_without_emitting_a_subject(self):
        previous = [{'subjectId': 1, 'centerX': .5, 'centerY': .5}]
        self.assertEqual(previous, recent_subjects(previous, 1_000_000_000, 1_400_000_000))
        self.assertEqual([], recent_subjects(previous, 1_000_000_000, 1_500_000_001))

    def test_unknown_or_reversed_clock_does_not_reuse_candidates(self):
        previous = [{'subjectId': 1}]
        self.assertEqual([], recent_subjects(previous, None, 10))
        self.assertEqual([], recent_subjects(previous, 100, 99))

    def test_prediction_is_bounded_and_only_uses_matching_unambiguous_history(self):
        previous = {'subjectId': 1, 'centerX': .5, 'centerY': .5, 'ambiguous': False}
        older = {'subjectId': 1, 'centerX': .4, 'centerY': .5, 'ambiguous': False}
        x, y = predicted_center(previous, older, 100_000_000, 50_000_000, 200_000_000)
        self.assertAlmostEqual(.55, x)
        self.assertAlmostEqual(.5, y)
        self.assertEqual((.5, .5), predicted_center(previous, older, 100_000_000, 50_000_000, 300_000_000))
        older['subjectId'] = 2
        self.assertEqual((.5, .5), predicted_center(previous, older, 100_000_000, 50_000_000, 200_000_000))
        older['subjectId'] = 1
        older['ambiguous'] = True
        self.assertEqual((.5, .5), predicted_center(previous, older, 100_000_000, 50_000_000, 200_000_000))


if __name__ == '__main__':
    unittest.main()
